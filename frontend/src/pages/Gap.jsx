import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { gapApi } from "../api";
import { Icon, PageHeader } from "../ui";
import { SAQ_LABEL } from "./Scope";
import { SECTIONS } from "./SurveyWizard";

const SEVERITY = { Critical: "Kritik", Medium: "Orta", Low: "Düşük" };
const SEVERITY_ICON = { Critical: "alert", Medium: "info", Low: "help" };
const EVIDENCE_KINDS = ["MissingEvidence", "AiNonCompliant", "AiPartial", "AuditorRejected"];
// Renk: kritik eksik varsa kırmızı, eksik varsa sarı, hiç eksik yoksa yeşil
const level = (open, critical) => (critical > 0 ? "bad" : open > 0 ? "mid" : "good");

// Tek bir eksik bile kaldıysa denetime hazır sayılmaz
function verdict(open, critical) {
  if (critical > 0) return { cls: "bad", icon: "alert", text: "Denetime hazır değil", sub: `${critical} kritik eksik var` };
  if (open > 0) return { cls: "mid", icon: "info", text: "Denetime hazır değil", sub: `${open} eksik kapatılmalı` };
  return { cls: "good", icon: "checkCircle", text: "Denetime hazır", sub: "Kapsamdaki tüm gereksinimler tamam" };
}

// Sayı açılışta 0'dan sayarak gelir
function useCountUp(target) {
  const [v, setV] = useState(0);
  useEffect(() => {
    let raf, start;
    const tick = (t) => {
      start ??= t;
      const p = Math.min((t - start) / 900, 1);
      setV(Math.round(target * (1 - (1 - p) ** 3)));
      if (p < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target]);
  return v;
}

// Büyük sayı: kaç gereksinim eksik. Altında kaç tanesinin tamam olduğu açıkça yazar.
function OpenCount({ data, critical }) {
  const shown = useCountUp(data.open);
  const applicable = data.total - data.notApplicable;
  return (
    <div className={`open-count ${level(data.open, critical)}`}>
      <span className="open-num">{shown}</span>
      <span className="open-lbl">{data.open === 0 ? "eksik yok" : "eksik gereksinim"}</span>
      <span className="open-sub">
        {applicable} gereksinimden <b>{data.complete}</b> tanesi tamam
        {data.notApplicable > 0 && <> · {data.notApplicable} uygulanamaz</>}
      </span>
    </div>
  );
}

function Kpi({ icon, tone, value, total, label }) {
  return (
    <div className={`kpi ${tone}`}>
      <span className="kpi-icon"><Icon name={icon} size={18} /></span>
      <span className="kpi-label">{label}</span>
      <span className="kpi-value">{value}{total !== undefined && <small>/{total}</small>}</span>
    </div>
  );
}

export default function Gap() {
  const nav = useNavigate();
  const [data, setData] = useState(null);
  const [error, setError] = useState("");
  const [severity, setSeverity] = useState("");
  const [section, setSection] = useState(null);

  useEffect(() => { gapApi.get().then(setData).catch((e) => setError(e.message)); }, []);

  if (error.includes("Kapsam")) return (
    <section>
      <PageHeader eyebrow="Adım 4 / 4" title="Gap Analizi" />
      <div className="card">
        <p>Gap analizi, SAQ tipinize göre sorulan gereksinimlere verdiğiniz beyanlardan hesaplanır.</p>
        <Link to="/scope"><button>Kapsam Belirlemeye Git <Icon name="arrowRight" size={16} /></button></Link>
      </div>
    </section>
  );
  if (error) return <div className="error">{error}</div>;
  if (!data) return <p className="muted">Yükleniyor…</p>;

  const count = (s) => data.findings.filter((f) => f.severity === s).length;
  const critical = count("Critical");
  const v = verdict(data.open, critical);
  const bySection = Object.fromEntries(data.sections.map((s) => [s.section, s]));
  const visible = data.findings.filter((f) => (!severity || f.severity === severity) && (!section || f.section === section));

  const act = (f) => (EVIDENCE_KINDS.includes(f.kind)
    ? nav(`/evidence?req=${encodeURIComponent(f.requirementCode)}`)
    : nav(`/survey?q=${f.questionId}`));

  return (
    <section>
      <PageHeader eyebrow="Adım 4 / 4" title="Gap Analizi"
        subtitle={`SAQ ${SAQ_LABEL[data.saqType]} · Beyanlarınız, kanıtlarınız ve otomatik analiz sonuçlarından anlık hesaplanır. Bir eksiği kapattığınızda sayılar hemen güncellenir.`} />

      <div className="gap-top">
        <div className="card score-card">
          <OpenCount data={data} critical={critical} />
          <div className={`verdict ${v.cls}`}>
            <Icon name={v.icon} size={18} />
            <span><strong>{v.text}</strong><span>{v.sub}</span></span>
          </div>
          {data.auditorApproved + data.auditorRejected > 0 && (
            <div className="auditor-line">
              <Icon name="shield" size={14} /> Denetçi: <b className="ok-text">{data.auditorApproved} onaylı</b>
              {data.auditorRejected > 0 && <> · <b className="error-text">{data.auditorRejected} reddedilen</b></>}
            </div>
          )}
        </div>
        <div className="kpis">
          <Kpi icon="checkCircle" tone="good" value={data.inPlace} total={data.total} label="Uygulanan kontrol" />
          <Kpi icon="alert" tone="bad" value={critical} label="Kritik bulgu" />
          <Kpi icon="fileX" tone="mid" value={data.missingEvidence} label="Eksik kanıt" />
          <Kpi icon="help" tone="neutral" value={data.total - data.answered} label="Cevaplanmamış" />
        </div>
      </div>

      <div className="section-head">
        <h2>PCI DSS gereksinimlerine göre durum</h2>
        <div className="legend">
          <span><i className="good" />Eksik yok</span><span><i className="mid" />Eksik var</span><span><i className="bad" />Kritik eksik var</span>
        </div>
      </div>
      <div className="heatmap">
        {Object.entries(SECTIONS).map(([n, name]) => {
          const s = bySection[n];
          if (!s) return (
            <div key={n} className="cell out" title="Bu SAQ kapsamında değil">
              <div className="cell-top"><span className="cell-no">{n}</span><span className="cell-score">Kapsam dışı</span></div>
              <span className="cell-name">{name}</span>
            </div>
          );
          const active = section === s.section;
          return (
            <button key={n} className={`cell ${level(s.open, s.criticalCount)} ${active ? "active" : ""}`}
              onClick={() => setSection(active ? null : s.section)} title={`${s.total} gereksinim · eksikleri süzmek için tıklayın`}>
              <div className="cell-top"><span className="cell-no">{n}</span>
                <span className="cell-score">{s.open === 0 ? "Tamam" : `${s.open} eksik`}</span></div>
              <span className="cell-name">{name}</span>
              <span className="cell-count">{s.complete}/{s.total - s.notApplicable} gereksinim tamam</span>
              {s.criticalCount > 0 && <span className="cell-crit"><Icon name="alert" size={12} /> {s.criticalCount} kritik</span>}
            </button>
          );
        })}
      </div>

      <div className="section-head">
        <h2>Eksikler ve notlar {section && <span className="filter-pill">Gereksinim {section}
          <button className="link" onClick={() => setSection(null)} aria-label="Filtreyi temizle">×</button></span>}</h2>
        <div className="chips">
          <button className={`chip ${!severity ? "on" : ""}`} onClick={() => setSeverity("")}>Tümü <b>{data.findings.length}</b></button>
          {Object.entries(SEVERITY).map(([k, label]) => (
            <button key={k} className={`chip ${k.toLowerCase()} ${severity === k ? "on" : ""}`} onClick={() => setSeverity(k)}>
              <i />{label} <b>{count(k)}</b>
            </button>
          ))}
        </div>
      </div>

      {visible.length === 0 && <div className="card empty"><Icon name="checkCircle" size={22} /> Bu filtrede bulgu yok.</div>}
      <div className="findings">
        {visible.map((f) => (
          <div key={f.questionId} className={`finding ${f.severity.toLowerCase()}`}>
            <span className="finding-icon"><Icon name={SEVERITY_ICON[f.severity]} size={18} /></span>
            <div className="finding-main">
              <div className="finding-meta">
                <span className={`sev ${f.severity.toLowerCase()}`}>{SEVERITY[f.severity]}</span>
                {f.kind.startsWith("Ai") && <span className="ai-tag"><Icon name="clipboard" size={12} /> Otomatik analiz</span>}
                {f.kind === "AuditorRejected" && <span className="ai-tag auditor-tag"><Icon name="shield" size={12} /> Denetçi</span>}
                <span className="code">{f.requirementCode}</span>
                <span className="finding-req">{f.requirementTitle}</span>
              </div>
              <strong>{f.issue}</strong>
              <span className="muted">{f.recommendation}</span>
            </div>
            <button className="ghost sm" onClick={() => act(f)}>
              {f.kind === "MissingEvidence" ? "Kanıt yükle" : EVIDENCE_KINDS.includes(f.kind) ? "Belgeyi gör" : "Beyanı güncelle"}
              <Icon name="arrowRight" size={14} />
            </button>
          </div>
        ))}
      </div>
    </section>
  );
}
