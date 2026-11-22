import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api";
import { Icon, Logo } from "../ui";

const POINTS = [
  { icon: "target", title: "Otomatik kapsam belirleme", text: "Ödeme kanallarınıza göre doğru SAQ tipi ve yalnızca ilgili sorular" },
  { icon: "clipboard", title: "Kanıtlı beyan", text: "Her beyan belgeyle desteklenir, belgelerin bütünlüğü kayıt altına alınır" },
  { icon: "chart", title: "Gap analizi paneli", text: "Kaç eksik kaldığı ve her birinin ne olduğu tek ekranda" },
];

export default function Login() {
  const nav = useNavigate();
  const [mode, setMode] = useState("login");
  const [form, setForm] = useState({ fullName: "", email: "", password: "", organizationName: "", isAuditor: false });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  // Denetçi hesabı her zaman denetçi paneline gider
  const run = async (call, to) => {
    setError(""); setBusy(true);
    try {
      const res = await call();
      localStorage.setItem("token", res.token);
      localStorage.setItem("org", res.organizationName);
      localStorage.setItem("role", res.role);
      nav(res.role === "Auditor" ? "/audit" : to);
    } catch (err) { setError(err.message); } finally { setBusy(false); }
  };
  const submit = (e) => {
    e.preventDefault();
    run(() => (mode === "login" ? authApi.login(form.email, form.password) : authApi.register(form)), "/survey");
  };
  // Demo kurumu her girişte hazır başlangıç durumuna sıfırlanır
  const demo = () => run(authApi.demo, "/scope");
  // Demo denetçi, demo kurumu sıfırlamadan onun kanıtlı beyanlarını inceler
  const demoAuditor = () => run(authApi.demoAuditor, "/audit");

  return (
    <div className="auth-page">
      <aside className="auth-hero">
        <Logo light />
        <div className="hero-copy">
          <span className="eyebrow">PCI DSS v4.0.1 · Ön değerlendirme platformu</span>
          <h1>Denetimden önce eksiklerinizi görün.</h1>
          <p>Kart ödemesi kabul eden kurumlar için uyum ön değerlendirmesi: beyan edin, kanıt yükleyin,
            hazırlık durumunuzu anında görün.</p>
          <ul className="hero-points">
            {POINTS.map((p) => (
              <li key={p.title}>
                <span className="hero-icon"><Icon name={p.icon} /></span>
                <span><strong>{p.title}</strong><span>{p.text}</span></span>
              </li>
            ))}
          </ul>
        </div>
        <div className="hero-foot"><Icon name="lock" size={14} /> Her kurum yalnızca kendi verisini görür</div>
      </aside>

      <main className="auth-main">
        <form className="auth" onSubmit={submit}>
          <h2>{mode === "login" ? "Tekrar hoş geldiniz" : form.isAuditor ? "Denetçi hesabı oluşturun" : "Kurum kaydı oluşturun"}</h2>
          <p className="muted">{mode === "login" ? "Hesabınıza giriş yapın."
            : form.isAuditor ? "Kurumların size verdiği erişimle kanıtlı beyanlarını inceleyin."
            : "Değerlendirmeye başlamak için kurumunuzu kaydedin."}</p>
          {mode === "register" && (
            <>
              <div className="segmented" role="radiogroup" aria-label="Hesap türü">
                {[[false, "Kurum"], [true, "Denetçi"]].map(([v, label]) => (
                  <button key={label} type="button" role="radio" aria-checked={form.isAuditor === v}
                    className={form.isAuditor === v ? "on" : ""} onClick={() => setForm({ ...form, isAuditor: v })}>{label}</button>
                ))}
              </div>
              <label>Ad Soyad<input required value={form.fullName} onChange={set("fullName")} /></label>
              <label>{form.isAuditor ? "Denetim Firması" : "Kurum Adı"}<input required value={form.organizationName} onChange={set("organizationName")} /></label>
            </>
          )}
          <label>E-posta<input type="email" required value={form.email} onChange={set("email")} placeholder="ad@kurum.com" /></label>
          <label>Parola<input type="password" required minLength={12} value={form.password} onChange={set("password")}
            placeholder="En az 12 karakter" /></label>
          {error && <div className="error">{error}</div>}
          <button className="block" disabled={busy}>{busy ? "Lütfen bekleyin…" : mode === "login" ? "Giriş yap" : "Kayıt ol"}</button>
          <button type="button" className="link" onClick={() => setMode(mode === "login" ? "register" : "login")}>
            {mode === "login" ? "Hesabınız yok mu? Kayıt olun" : "Zaten hesabınız var mı? Giriş yapın"}
          </button>
          <div className="divider"><span>veya</span></div>
          <button type="button" className="demo-btn" disabled={busy} onClick={demo}>
            <span className="hero-icon"><Icon name="building" /></span>
            <span><strong>Demo kurumla giriş</strong><span>Örnek e-ticaret kurumu, hazır beyan ve kanıtlarla açılır</span></span>
            <Icon name="arrowRight" />
          </button>
          <button type="button" className="demo-btn" disabled={busy} onClick={demoAuditor}>
            <span className="hero-icon"><Icon name="shield" /></span>
            <span><strong>Demo denetçiyle giriş</strong><span>Demo kurumun kanıtlı beyanlarını denetçi gözüyle inceleyin</span></span>
            <Icon name="arrowRight" />
          </button>
        </form>
      </main>
    </div>
  );
}
