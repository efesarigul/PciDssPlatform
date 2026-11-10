import { Fragment, useCallback, useEffect, useState } from "react";
import { useDropzone } from "react-dropzone";
import { useNavigate, useSearchParams } from "react-router-dom";
import { filesApi, surveyApi } from "../api";
import { Icon, PageHeader } from "../ui";

const fmtSize = (b) => (b > 1048576 ? `${(b / 1048576).toFixed(1)} MB` : `${Math.ceil(b / 1024)} KB`);
const VERDICT = { Compliant: "Uyumlu", Partial: "Kısmen uyumlu", NonCompliant: "Uyumsuz" };
const inQueue = (f) => f.requirementCode && (f.status === "Pending" || f.status === "Processing");

function StatusBadge({ f }) {
  if (!f.requirementCode) return <span className="badge pending" title="YZ analizi için gereksinim seçilmeli">Gereksinim seçilmedi</span>;
  if (f.status === "Pending") return <span className="badge pending">Analiz sırada</span>;
  if (f.status === "Processing") return <span className="badge processing"><span className="spinner" />YZ analiz ediyor</span>;
  if (f.status === "Failed") return <span className="badge failed">Analiz başarısız</span>;
  return <span className={`badge verdict-${f.ai.verdict.toLowerCase()}`}>{VERDICT[f.ai.verdict]}</span>;
}

function AiDetail({ f, onRetry }) {
  if (inQueue(f)) return <p className="muted">Belge parçalara ayrılıyor, gereksinimle en ilgili bölümler bulunup YZ'ye soruluyor…</p>;
  if (f.status === "Failed") return (
    <div className="ai-detail">
      <p className="error-text">{f.ai?.summary}</p>
      <button className="ghost" onClick={onRetry}>Yeniden analiz et</button>
    </div>
  );
  if (!f.ai) return null;
  return (
    <div className={`ai-detail verdict-${f.ai.verdict?.toLowerCase()}`}>
      <div className="ai-head">
        <span className="ai-tag"><Icon name="sparkles" size={12} /> YZ değerlendirmesi</span>
        <span className="muted small">{f.ai.model} · güven %{Math.round((f.ai.confidence ?? 0) * 100)}</span>
        {f.ai.model !== "demo" && <button className="link" onClick={onRetry}>Yeniden analiz et</button>}
      </div>
      <p>{f.ai.summary}</p>
      <div className="ai-cols">
        {f.ai.gaps.length > 0 && <div><h4>Eksikler</h4><ul>{f.ai.gaps.map((g) => <li key={g}>{g}</li>)}</ul></div>}
        {f.ai.quotes.length > 0 && <div><h4>Belgeden dayanak</h4>{f.ai.quotes.map((q) => <blockquote key={q}>“{q}”</blockquote>)}</div>}
      </div>
    </div>
  );
}

export default function Evidence() {
  const nav = useNavigate();
  const [params] = useSearchParams();
  const [files, setFiles] = useState([]);
  const [requirements, setRequirements] = useState([]);
  // Gap analizinden ?req=<kod> ile gelindiyse gereksinim önceden seçili olur
  const [reqCode, setReqCode] = useState(params.get("req") ?? "");
  const [uploads, setUploads] = useState([]); // {name, progress, error}
  const [open, setOpen] = useState(new Set()); // ayrıntısı açık belgeler

  useEffect(() => {
    filesApi.list().then(setFiles);
    // Kapsam belirlenmemişse liste boş kalır; yalnızca genel belge yüklenebilir
    surveyApi.questions().then(({ questions: qs }) => {
      const uniq = [...new Map(qs.map((q) => [q.requirementCode, q])).values()];
      setRequirements(uniq);
    }).catch(() => setRequirements([]));
  }, []);

  // Analiz sırasında olan belge varken listeyi tazele
  const waiting = files.some(inQueue);
  useEffect(() => {
    if (!waiting) return;
    const t = setInterval(() => filesApi.list().then(setFiles), 2500);
    return () => clearInterval(t);
  }, [waiting]);

  const toggle = (id, force) => setOpen((s) => {
    const n = new Set(s);
    (force ?? !n.has(id)) ? n.add(id) : n.delete(id);
    return n;
  });

  const retry = async (id) => {
    await filesApi.reanalyze(id);
    setFiles((fs) => fs.map((f) => (f.id === id ? { ...f, status: "Pending", ai: null } : f)));
  };

  const onDrop = useCallback(async (accepted, rejected) => {
    rejected.forEach((r) => setUploads((u) => [...u, { name: r.file.name, progress: 0, error: r.errors[0]?.message }]));
    for (const file of accepted) {
      setUploads((u) => [...u, { name: file.name, progress: 0 }]);
      const setP = (patch) => setUploads((u) => u.map((x) => (x.name === file.name ? { ...x, ...patch } : x)));
      try {
        const saved = await filesApi.upload(file, reqCode, (p) => setP({ progress: p }));
        setFiles((f) => [saved, ...f]);
        if (saved.requirementCode) toggle(saved.id, true); // YZ sonucu gelince doğrudan görünsün
        setUploads((u) => u.filter((x) => x.name !== file.name));
      } catch (e) { setP({ error: e.message }); }
    }
  }, [reqCode]);

  const hint = requirements.find((r) => r.requirementCode === reqCode)?.evidenceHint;
  const counts = files.reduce((c, f) => (f.status === "Completed" && f.ai?.verdict ? { ...c, [f.ai.verdict]: (c[f.ai.verdict] ?? 0) + 1 } : c), {});

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    maxSize: 10 * 1024 * 1024,
    accept: {
      "application/pdf": [".pdf"],
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document": [".docx"],
      "text/plain": [".txt", ".md"],
    },
  });

  return (
    <section>
      <PageHeader eyebrow="Adım 3 / 4" title="Kanıt Yükleme"
        subtitle="Politika, rapor ve yapılandırma çıktılarınızı yükleyin. Bir gereksinimle ilişkilendirilen her belge yapay zekâ ile okunur ve gereksinimi karşılayıp karşılamadığı değerlendirilir.">
        <button onClick={() => nav("/gap")}>Gap Analizini Gör <Icon name="arrowRight" size={16} /></button>
      </PageHeader>

      <div className="card upload-card">
        <div className="upload-fields">
          <label>İlgili gereksinim
            <select value={reqCode} onChange={(e) => setReqCode(e.target.value)}>
              <option value="">Genel belge (YZ analizi yapılmaz)</option>
              {requirements.map((r) => <option key={r.requirementCode} value={r.requirementCode}>{r.requirementCode} · {r.requirementTitle}</option>)}
            </select>
          </label>
          {hint
            ? <div className="hint"><Icon name="file" size={16} /><span>Beklenen kanıt: <strong>{hint}</strong></span></div>
            : <div className="hint neutral"><Icon name="sparkles" size={16} /><span>YZ analizi için belgeyi bir gereksinimle ilişkilendirin.</span></div>}
        </div>

        <div {...getRootProps()} className={`dropzone ${isDragActive ? "active" : ""}`}>
          <input {...getInputProps()} />
          <span className="drop-icon"><Icon name="upload" size={22} /></span>
          <strong>{isDragActive ? "Bırakın…" : "Dosyaları buraya sürükleyin"}</strong>
          <span className="muted">veya seçmek için tıklayın · PDF, DOCX, TXT, MD · en fazla 10 MB</span>
        </div>

        {uploads.map((u) => (
          <div key={u.name} className={`upload-row ${u.error ? "err" : ""}`}>
            <span>{u.name}</span>
            {u.error ? <span className="error-text">{u.error}</span>
              : <div className="progress small"><div style={{ width: `${u.progress}%` }} /></div>}
          </div>
        ))}
      </div>

      <div className="card table-card">
        <div className="table-head">
          <h2>Yüklenen belgeler</h2>
          <div className="table-stats">
            <span>{files.length} belge</span>
            {counts.Compliant > 0 && <span className="badge verdict-compliant">{counts.Compliant} uyumlu</span>}
            {counts.Partial > 0 && <span className="badge verdict-partial">{counts.Partial} kısmen</span>}
            {counts.NonCompliant > 0 && <span className="badge verdict-noncompliant">{counts.NonCompliant} uyumsuz</span>}
          </div>
        </div>
      <table className="table">
        <thead><tr><th>Dosya</th><th>Gereksinim</th><th>Boyut</th><th>YZ Analizi</th><th>Tarih</th></tr></thead>
        <tbody>
          {files.length === 0 && <tr><td colSpan={5} className="muted center">Henüz belge yüklenmedi.</td></tr>}
          {files.map((f) => (
            <Fragment key={f.id}>
              <tr className={f.requirementCode ? "clickable" : ""} onClick={() => f.requirementCode && toggle(f.id)}>
                <td>
                  <span className="file-cell">
                    <span className={`chev ${open.has(f.id) ? "open" : ""}`}>{f.requirementCode ? "›" : ""}</span>
                    <Icon name="file" size={16} className="file-icon" />
                    <span className="file-name" title={f.fileName}>{f.fileName}</span>
                  </span>
                </td>
                <td>{f.requirementCode ? <span className="code">{f.requirementCode}</span> : <span className="muted">—</span>}</td>
                <td className="muted nowrap">{fmtSize(f.sizeBytes)}</td>
                <td><StatusBadge f={f} /></td>
                <td className="muted nowrap">{new Date(f.uploadedAt).toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" })}</td>
              </tr>
              {open.has(f.id) && (
                <tr className="detail-row"><td colSpan={5}><AiDetail f={f} onRetry={() => retry(f.id)} /></td></tr>
              )}
            </Fragment>
          ))}
        </tbody>
      </table>
      </div>
    </section>
  );
}
