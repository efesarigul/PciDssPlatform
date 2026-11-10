import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { gapApi } from "../api";
import { Icon, PageHeader } from "../ui";
import { SAQ_LABEL } from "./Scope";
import { SECTIONS } from "./SurveyWizard";

const SEVERITY = { Critical: "Kritik", Medium: "Orta", Low: "Düşük" };
const SEVERITY_ICON = { Critical: "alert", Medium: "info", Low: "help" };
const EVIDENCE_KINDS = ["MissingEvidence", "AiNonCompliant", "AiPartial"];
const level = (score) => (score >= 90 ? "good" : score >= 70 ? "mid" : "bad");

// Kritik bulgu varken skor ne olursa olsun denetime hazır sayılmaz
function verdict(score, critical) {
  if (critical > 0) return { cls: "bad", icon: "alert", text: "Denetime hazır değil", sub: `${critical} kritik bulgu kapatılmalı` };
  if (score >= 90) return { cls: "good", icon: "checkCircle", text: "Denetime hazır", sub: "Kritik bulgu yok" };
  return { cls: "mid", icon: "info", text: "Kısmen hazır", sub: "Eksikler tamamlanmalı" };
}

// Skor açılışta 0'dan sayarak gelir
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

function ScoreRing({ score }) {
  const shown = useCountUp(score);
  const r = 54, c = 2 * Math.PI * r;
  return (
    <svg className={`ring ${level(score)}`} viewBox="0 0 140 140" role="img" aria-label={`Uyum skoru %${score}`}>
      <circle cx="70" cy="70" r={r} className="track" />
      <circle cx="70" cy="70" r={r} className="value" strokeDasharray={c} strokeDashoffset={c * (1 - shown / 100)} />
      <text x="70" y="70" className="pct">%{shown}</text>
      <text x="70" y="92" className="lbl">uyum skoru</text>
    </svg>
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
  const v = verdict(data.score, critical);
  const bySection = Object.fromEntries(data.sections.map((s) => [s.section, s]));
  const visible = data.findings.filter((f) => (!severity || f.severity === severity) && (!section || f.section === section));

  const act = (f) => (EVIDENCE_KINDS.includes(f.kind)
    ? nav(`/evidence?req=${encodeURIComponent(f.requirementCode)}`)
    : nav(`/survey?q=${f.questionId}`));

  return (
    <section>
      <PageHeader eyebrow="Adım 4 / 4" title="Gap Analizi"
        subtitle={`SAQ ${SAQ_LABEL[data.saqType]} · Beyanlarınız, kanıtlarınız ve YZ değerlendirmelerinden anlık hesaplanır. Bir bulguyu kapattığınızda skor hemen güncellenir.`} />

      <div className="gap-top">
        <div className="card score-card">
          <ScoreRing score={data.score} />
          <div className={`verdict ${v.cls}`}>
            <Icon name={v.icon} size={18} />
            <span><strong>{v.text}</strong><span>{v.sub}</span></span>
          </div>
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
          <span><i className="good" />%90+</span><span><i className="mid" />%70–89</span><span><i className="bad" />%70 altı</span>
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
            <button key={n} className={`cell ${level(s.score)} ${active ? "active" : ""}`}
              onClick={() => setSection(active ? null : s.section)} title={`${s.total} gereksinim · bulguları süzmek için tıklayın`}>
              <div className="cell-top"><span className="cell-no">{n}</span><span className="cell-score">%{s.score}</span></div>
              <span className="cell-name">{name}</span>
              <div className="cell-bar"><div style={{ width: `${s.score}%` }} /></div>
              {s.criticalCount > 0 && <span className="cell-crit"><Icon name="alert" size={12} /> {s.criticalCount} kritik</span>}
            </button>
          );
        })}
      </div>

      <div className="section-head">
        <h2>Bulgular {section && <span className="filter-pill">Gereksinim {section}
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
                {f.kind.startsWith("Ai") && <span className="ai-tag"><Icon name="sparkles" size={12} /> YZ</span>}
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
