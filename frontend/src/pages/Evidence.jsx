import { useCallback, useEffect, useState } from "react";
import { useDropzone } from "react-dropzone";
import { filesApi, surveyApi } from "../api";

const fmtSize = (b) => (b > 1048576 ? `${(b / 1048576).toFixed(1)} MB` : `${Math.ceil(b / 1024)} KB`);
const STATUS = { Pending: "Analiz bekliyor", Processing: "Analiz ediliyor", Completed: "Tamamlandı", Failed: "Hata" };

export default function Evidence() {
  const [files, setFiles] = useState([]);
  const [requirements, setRequirements] = useState([]);
  const [reqCode, setReqCode] = useState("");
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
      <h2>Kanıt Yükleme</h2>
      <p className="muted">Güvenlik politikaları, sistem raporları ve konfigürasyon çıktılarını yükleyin. Faz 2'de bu belgeler RAG servisiyle analiz edilecek.</p>

      <label className="inline">İlgili gereksinim
        <select value={reqCode} onChange={(e) => setReqCode(e.target.value)}>
          <option value="">— Genel belge —</option>
          {requirements.map((r) => <option key={r.requirementCode} value={r.requirementCode}>{r.requirementCode} · {r.requirementTitle}</option>)}
        </select>
      </label>
      {requirements.find((r) => r.requirementCode === reqCode)?.evidenceHint && (
        <div className="hint">📎 Beklenen kanıt: <strong>{requirements.find((r) => r.requirementCode === reqCode).evidenceHint}</strong></div>
      )}

      <div {...getRootProps()} className={`dropzone ${isDragActive ? "active" : ""}`}>
        <input {...getInputProps()} />
        <div className="drop-icon">⬆</div>
        <strong>{isDragActive ? "Bırakın…" : "Dosyaları buraya sürükleyin"}</strong>
        <span className="muted">veya seçmek için tıklayın · PDF, DOCX, TXT · en fazla 10 MB</span>
      </div>

      {uploads.map((u) => (
        <div key={u.name} className={`upload-row ${u.error ? "err" : ""}`}>
          <span>{u.name}</span>
          {u.error ? <span className="error-text">{u.error}</span>
            : <div className="progress small"><div style={{ width: `${u.progress}%` }} /></div>}
        </div>
      ))}

      <table className="table">
        <thead><tr><th>Dosya</th><th>Gereksinim</th><th>Boyut</th><th>Durum</th><th>Tarih</th></tr></thead>
        <tbody>
          {files.length === 0 && <tr><td colSpan={5} className="muted center">Henüz belge yüklenmedi.</td></tr>}
          {files.map((f) => (
            <tr key={f.id}>
              <td>{f.fileName}</td>
              <td>{f.requirementCode ?? "—"}</td>
              <td>{fmtSize(f.sizeBytes)}</td>
              <td><span className={`badge ${f.status.toLowerCase()}`}>{STATUS[f.status]}</span></td>
              <td>{new Date(f.uploadedAt).toLocaleString("tr-TR")}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
