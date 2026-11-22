import { useCallback, useEffect, useState } from "react";
import { useDropzone } from "react-dropzone";
import { useNavigate, useSearchParams } from "react-router-dom";
import { filesApi, surveyApi } from "../api";
import { Icon, PageHeader } from "../ui";

const fmtSize = (b) => (b > 1048576 ? `${(b / 1048576).toFixed(1)} MB` : `${Math.ceil(b / 1024)} KB`);

export default function Evidence() {
  const nav = useNavigate();
  const [params] = useSearchParams();
  const [files, setFiles] = useState([]);
  const [requirements, setRequirements] = useState([]);
  // Gap analizinden ?req=<kod> ile gelindiyse gereksinim önceden seçili olur
  const [reqCode, setReqCode] = useState(params.get("req") ?? "");
  const [uploads, setUploads] = useState([]); // {name, progress, error}

  useEffect(() => {
    filesApi.list().then(setFiles);
    // Kapsam belirlenmemişse liste boş kalır; yalnızca genel belge yüklenebilir
    surveyApi.questions().then(({ questions: qs }) => {
      const uniq = [...new Map(qs.map((q) => [q.requirementCode, q])).values()];
      setRequirements(uniq);
    }).catch(() => setRequirements([]));
  }, []);

  const onDrop = useCallback(async (accepted, rejected) => {
    rejected.forEach((r) => setUploads((u) => [...u, { name: r.file.name, progress: 0, error: r.errors[0]?.message }]));
    for (const file of accepted) {
      setUploads((u) => [...u, { name: file.name, progress: 0 }]);
      const setP = (patch) => setUploads((u) => u.map((x) => (x.name === file.name ? { ...x, ...patch } : x)));
      try {
        const saved = await filesApi.upload(file, reqCode, (p) => setP({ progress: p }));
        setFiles((f) => [saved, ...f]);
        setUploads((u) => u.filter((x) => x.name !== file.name));
      } catch (e) { setP({ error: e.message }); }
    }
  }, [reqCode]);

  const hint = requirements.find((r) => r.requirementCode === reqCode)?.evidenceHint;
  const linked = files.filter((f) => f.requirementCode).length;

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
        subtitle="Politika, rapor ve yapılandırma çıktılarınızı yükleyin. Bir gereksinimle ilişkilendirilen belge o gereksinimin kanıtı sayılır; erişim verdiğiniz denetçi bu kanıtları inceleyebilir.">
        <button onClick={() => nav("/gap")}>Gap Analizini Gör <Icon name="arrowRight" size={16} /></button>
      </PageHeader>

      <div className="card upload-card">
        <div className="upload-fields">
          <label>İlgili gereksinim
            <select value={reqCode} onChange={(e) => setReqCode(e.target.value)}>
              <option value="">Genel belge (bir gereksinime bağlanmaz)</option>
              {requirements.map((r) => <option key={r.requirementCode} value={r.requirementCode}>{r.requirementCode} · {r.requirementTitle}</option>)}
            </select>
          </label>
          {hint
            ? <div className="hint"><Icon name="file" size={16} /><span>Beklenen kanıt: <strong>{hint}</strong></span></div>
            : <div className="hint neutral"><Icon name="clipboard" size={16} /><span>Belgenin kanıt sayılması için onu bir gereksinimle ilişkilendirin.</span></div>}
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
            {linked > 0 && <span className="badge verdict-compliant">{linked} gereksinime bağlı</span>}
          </div>
        </div>
      <table className="table">
        <thead><tr><th>Dosya</th><th>Gereksinim</th><th>Boyut</th><th>Tarih</th></tr></thead>
        <tbody>
          {files.length === 0 && <tr><td colSpan={4} className="muted center">Henüz belge yüklenmedi.</td></tr>}
          {files.map((f) => (
            <tr key={f.id}>
              <td>
                <span className="file-cell">
                  <Icon name="file" size={16} className="file-icon" />
                  <span className="file-name" title={f.fileName}>{f.fileName}</span>
                </span>
              </td>
              <td>{f.requirementCode ? <span className="code">{f.requirementCode}</span> : <span className="muted">—</span>}</td>
              <td className="muted nowrap">{fmtSize(f.sizeBytes)}</td>
              <td className="muted nowrap">{new Date(f.uploadedAt).toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" })}</td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>
    </section>
  );
}
