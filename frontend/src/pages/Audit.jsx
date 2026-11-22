import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { auditApi } from "../api";
import { Checklist, Icon, PageHeader } from "../ui";
import { SAQ_LABEL } from "./Scope";
import { ANSWER_LABEL } from "./SurveyWizard";

const VERDICT = { Compliant: "Uyumlu", Partial: "Kısmen uyumlu", NonCompliant: "Uyumsuz" };
const fmtDate = (d) => new Date(d).toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });

// Denetçi kuyruğundaki bir kalemin durumu
const STATE = {
  Ready: { label: "İnceleme bekliyor", cls: "state-ready" },
  Stale: { label: "Kanıt değişti, yeniden incele", cls: "state-stale" },
  WaitingSystem: { label: "Sistem kontrolünde", cls: "state-waiting" },
  Approved: { label: "Onaylandı", cls: "state-approved" },
  Rejected: { label: "Reddedildi", cls: "state-rejected" },
};
const FILTERS = [
  { key: "todo", label: "İncelenecek", match: (s) => s === "Ready" || s === "Stale" },
  { key: "Approved", label: "Onaylanan", match: (s) => s === "Approved" },
  { key: "Rejected", label: "Reddedilen", match: (s) => s === "Rejected" },
  { key: "WaitingSystem", label: "Sistem kontrolünde", match: (s) => s === "WaitingSystem" },
  { key: "all", label: "Tümü", match: () => true },
];

// ---------------- Kurum listesi ----------------
export function AuditOrgs() {
  const nav = useNavigate();
  const [orgs, setOrgs] = useState(null);
  const [error, setError] = useState("");
  useEffect(() => { auditApi.orgs().then(setOrgs).catch((e) => setError(e.message)); }, []);

  return (
    <section>
      <PageHeader eyebrow="Denetçi paneli" title="Kurumlar"
        subtitle="Size erişim veren kurumlar. Kurumların kanıtlı beyanları önce sistem tarafından otomatik kontrol edilir, ardından incelemeniz için kuyruğa düşer." />
      {error && <div className="error">{error}</div>}
      <div className="card table-card">
        <table className="table">
          <thead><tr><th>Kurum</th><th>SAQ</th><th>Kanıtlı beyan</th><th>İncelenecek</th><th>Onaylı / Reddedilen</th></tr></thead>
          <tbody>
            {orgs === null && !error && <tr><td colSpan={5} className="muted center">Yükleniyor…</td></tr>}
            {orgs?.length === 0 && (
              <tr><td colSpan={5} className="muted center">
                Henüz size erişim veren kurum yok. Kurumlar "Denetçi Erişimi" ekranından e-posta adresinizle erişim verebilir.
              </td></tr>
            )}
            {orgs?.map((o) => (
              <tr key={o.organizationId} className="clickable" onClick={() => nav(`/audit/${o.organizationId}`)}>
                <td>
                  <span className="file-cell"><Icon name="building" size={16} className="file-icon" />
                    <strong className="file-name">{o.name}</strong></span>
                </td>
                <td>{o.saqType ? <span className="code">{SAQ_LABEL[o.saqType]}</span> : <span className="muted">Kapsam yok</span>}</td>
                <td>{o.evidenced}</td>
                <td>{o.ready > 0 ? <span className="badge state-ready">{o.ready} kalem</span> : <span className="muted">—</span>}</td>
                <td className="nowrap"><span className="ok-text">{o.approved}</span> / <span className="error-text">{o.rejected}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

// ---------------- Kurum inceleme ekranı ----------------
export function AuditReview() {
  const { orgId } = useParams();
  const [data, setData] = useState(null);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState("todo");
  const [open, setOpen] = useState(null); // açık kalemin soru id'si

  const load = () => auditApi.org(orgId).then(setData).catch((e) => setError(e.message));
  useEffect(() => { load(); }, [orgId]);

  // Sistem kontrolü süren kalem varken listeyi tazele
  const waiting = data?.items.some((i) => i.state === "WaitingSystem");
  useEffect(() => {
    if (!waiting) return;
    const t = setInterval(load, 4000);
    return () => clearInterval(t);
  }, [waiting, orgId]);

  if (error && !data) return (
    <section>
      <Link to="/audit" className="back-link"><Icon name="arrowLeft" size={14} /> Kurumlar</Link>
      <div className="error">{error}</div>
    </section>
  );
  if (!data) return <p className="muted">Yükleniyor…</p>;

  const f = FILTERS.find((x) => x.key === filter);
  const visible = data.items.filter((i) => f.match(i.state));
  const count = (key) => data.items.filter((i) => FILTERS.find((x) => x.key === key).match(i.state)).length;

  return (
    <section>
      <Link to="/audit" className="back-link"><Icon name="arrowLeft" size={14} /> Kurumlar</Link>
      <PageHeader eyebrow={`SAQ ${SAQ_LABEL[data.saqType]}`} title={data.name}
        subtitle="Kurumun beyanını, yüklediği belgeleri ve sistemin otomatik değerlendirmesini inceleyin. Kararınız kurumun gap analizine yansır; ret gerekçeniz kuruma kritik bulgu olarak gösterilir." />

      {data.declarationOnly > 0 && (
        <div className="hint neutral audit-hint">
          <Icon name="info" size={16} />
          <span><strong>{data.declarationOnly} gereksinim</strong> yalnızca beyanla "Uygulanıyor" işaretlenmiş, kanıt yüklenmemiş.
            Kanıtsız beyanlar inceleme kuyruğuna girmez.</span>
        </div>
      )}
      {error && <div className="error">{error}</div>}

      <div className="section-head">
        <h2>Kanıtlı beyanlar</h2>
        <div className="chips">
          {FILTERS.map((x) => (
            <button key={x.key} className={`chip ${filter === x.key ? "on" : ""}`} onClick={() => setFilter(x.key)}>
              {x.label} <b>{count(x.key)}</b>
            </button>
          ))}
        </div>
      </div>

      {visible.length === 0 && <div className="card empty"><Icon name="checkCircle" size={22} /> Bu filtrede kalem yok.</div>}
      <div className="review-list">
        {visible.map((item) => (
          <ReviewItem key={item.question.id} item={item} orgId={orgId} open={open === item.question.id}
            onToggle={() => setOpen(open === item.question.id ? null : item.question.id)}
            onSaved={load} />
        ))}
      </div>
    </section>
  );
}

function ReviewItem({ item, orgId, open, onToggle, onSaved }) {
  const { question: q, state, files } = item;
  const st = STATE[state];
  const r = q.review;
  const [note, setNote] = useState(r?.note ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(""); // hata kalemin içinde gösterilir; sayfa başı görünmüyor olabilir

  const decide = async (decision) => {
    setBusy(true);
    setError("");
    try { await auditApi.review(orgId, q.id, decision, note); onSaved(); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  };

  // Dosya oturum belirteciyle indirilir; PDF ve metin yeni sekmede açılır, DOCX indirilir
  const openFile = async (f) => {
    const inline = !/\.docx$/i.test(f.fileName);
    const w = inline ? window.open("", "_blank") : null;
    try {
      const url = URL.createObjectURL(await auditApi.file(orgId, f.id));
      if (w) w.location.href = url;
      else { const a = document.createElement("a"); a.href = url; a.download = f.fileName; a.click(); }
      setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch (e) { w?.close(); setError(e.message); }
  };

  return (
    <div className={`review-item ${open ? "open" : ""}`}>
      <button className="review-head" onClick={onToggle} aria-expanded={open}>
        <span className={`chev ${open ? "open" : ""}`}>›</span>
        <span className="code">{q.requirementCode}</span>
        <span className="review-title">{q.requirementTitle}</span>
        {q.evidenceAi && <span className={`badge verdict-${q.evidenceAi.verdict.toLowerCase()}`}><Icon name="clipboard" size={12} /> {VERDICT[q.evidenceAi.verdict]}</span>}
        <span className={`badge ${st.cls}`}>{st.label}</span>
      </button>

      {open && (
        <div className="review-body">
          <div className="review-grid">
            <div className="review-block">
              <h4>Kurum beyanı</h4>
              <p className="review-q">{q.text}</p>
              <p><strong>{ANSWER_LABEL[q.currentAnswer]}</strong></p>
              {q.currentComment && <blockquote>{q.currentComment}</blockquote>}
              {q.evidenceHint && <p className="muted small">Beklenen kanıt: {q.evidenceHint}</p>}
            </div>
            <div className="review-block">
              <h4>Belgeler ve sistem kontrolü</h4>
              {files.map((f) => (
                <div key={f.id} className="doc">
                  <div className="doc-row">
                    <Icon name="file" size={16} className="file-icon" />
                    <span className="file-name" title={f.fileName}>{f.fileName}</span>
                    <DocStatus f={f} />
                    <button className="link sm" onClick={() => openFile(f)}><Icon name="external" size={14} /> Aç</button>
                  </div>
                  {/* Kontrol listesi yoksa (eski analiz) belge bazlı özet gösterilir */}
                  {!q.evidenceAi?.checks && f.ai?.summary && <p className="doc-summary">{f.ai.summary}</p>}
                  {!q.evidenceAi?.checks && f.ai?.gaps?.length > 0 && <ul className="doc-gaps">{f.ai.gaps.map((g) => <li key={g}>{g}</li>)}</ul>}
                  <span className="muted small">Yüklendi {fmtDate(f.uploadedAt)}{f.ai?.model ? ` · ${f.ai.model}` : ""}</span>
                </div>
              ))}
              {q.evidenceAi?.summary && q.evidenceAi.checks && <p className="doc-summary">{q.evidenceAi.summary}</p>}
            </div>
          </div>

          {state !== "WaitingSystem" && <Checklist checks={q.evidenceAi?.checks} />}

          {r && (
            <div className={`review-last ${r.decision === "Approved" ? "ok" : "bad"}`}>
              <Icon name={r.decision === "Approved" ? "checkCircle" : "alert"} size={16} />
              <span>
                <strong>{r.decision === "Approved" ? "Onaylandı" : "Reddedildi"}</strong> · {r.auditorName} · {fmtDate(r.reviewedAt)}
                {r.stale && <em> · Bu karardan sonra kanıt değişti; karar geçersiz sayılıyor.</em>}
                {r.note && <span className="review-note">{r.note}</span>}
              </span>
            </div>
          )}

          {state === "WaitingSystem" ? (
            <div className="hint neutral"><span className="spinner" /><span>Belgelerin otomatik kontrolü tamamlanınca karar verebilirsiniz.</span></div>
          ) : (
            <div className="review-actions">
              <label>Denetçi notu <span className="muted small">(ret için zorunlu; kurum bu notu görür)</span>
                <textarea rows={2} value={note} onChange={(e) => setNote(e.target.value)} maxLength={2000}
                  placeholder="Örn. Kural setinde gözden geçirme tarihi yok; son 6 aya ait imzalı tutanağı yükleyin." />
              </label>
              {error && <div className="error">{error}</div>}
              <div className="review-buttons">
                <button className="danger" disabled={busy} onClick={() => decide("Rejected")}><Icon name="x" size={16} /> Reddet</button>
                <button className="approve" disabled={busy} onClick={() => decide("Approved")}><Icon name="check" size={16} /> Onayla</button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function DocStatus({ f }) {
  if (f.status === "Pending" || f.status === "Processing") return <span className="badge processing"><span className="spinner" />Analiz ediliyor</span>;
  if (f.status === "Failed") return <span className="badge failed">Analiz başarısız</span>;
  return <span className={`badge verdict-${f.ai.verdict.toLowerCase()}`}>{VERDICT[f.ai.verdict]}</span>;
}
