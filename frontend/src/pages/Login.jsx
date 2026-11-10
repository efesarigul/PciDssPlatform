import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api";
import { Icon, Logo } from "../ui";

const POINTS = [
  { icon: "target", title: "Otomatik kapsam belirleme", text: "Ödeme kanallarınıza göre doğru SAQ tipi ve yalnızca ilgili sorular" },
  { icon: "sparkles", title: "YZ ile kanıt analizi", text: "Belgeleriniz gereksinimlerle anlamsal olarak karşılaştırılır" },
  { icon: "chart", title: "Gap analizi paneli", text: "Uyum skoru ve önceliklendirilmiş bulgular tek ekranda" },
];

export default function Login() {
  const nav = useNavigate();
  const [mode, setMode] = useState("login");
  const [form, setForm] = useState({ fullName: "", email: "", password: "", organizationName: "" });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  const run = async (call, to) => {
    setError(""); setBusy(true);
    try {
      const res = await call();
      localStorage.setItem("token", res.token);
      localStorage.setItem("org", res.organizationName);
      nav(to);
    } catch (err) { setError(err.message); } finally { setBusy(false); }
  };
  const submit = (e) => {
    e.preventDefault();
    run(() => (mode === "login" ? authApi.login(form.email, form.password) : authApi.register(form)), "/survey");
  };
  // Demo kurumu her girişte hazır başlangıç durumuna sıfırlanır
  const demo = () => run(authApi.demo, "/scope");

  return (
    <div className="auth-page">
      <aside className="auth-hero">
        <Logo light />
        <div className="hero-copy">
          <span className="eyebrow">PCI DSS v4.0.1 · Yapay zekâ destekli</span>
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
          <h2>{mode === "login" ? "Tekrar hoş geldiniz" : "Kurum kaydı oluşturun"}</h2>
          <p className="muted">{mode === "login" ? "Hesabınıza giriş yapın." : "Değerlendirmeye başlamak için kurumunuzu kaydedin."}</p>
          {mode === "register" && (
            <>
              <label>Ad Soyad<input required value={form.fullName} onChange={set("fullName")} /></label>
              <label>Kurum Adı<input required value={form.organizationName} onChange={set("organizationName")} /></label>
            </>
          )}
          <label>E-posta<input type="email" required value={form.email} onChange={set("email")} placeholder="ad@kurum.com" /></label>
          <label>Parola<input type="password" required minLength={12} value={form.password} onChange={set("password")}
            placeholder="En az 12 karakter" /></label>
          {error && <div className="error">{error}</div>}
          <button className="block" disabled={busy}>{busy ? "Lütfen bekleyin…" : mode === "login" ? "Giriş yap" : "Kayıt ol"}</button>
          <button type="button" className="link" onClick={() => setMode(mode === "login" ? "register" : "login")}>
            {mode === "login" ? "Hesabınız yok mu? Kurum kaydı oluşturun" : "Zaten hesabınız var mı? Giriş yapın"}
          </button>
          <div className="divider"><span>veya</span></div>
          <button type="button" className="demo-btn" disabled={busy} onClick={demo}>
            <span className="hero-icon"><Icon name="sparkles" /></span>
            <span><strong>Demo kurumla giriş</strong><span>Örnek e-ticaret kurumu, hazır beyan ve kanıtlarla açılır</span></span>
            <Icon name="arrowRight" />
          </button>
        </form>
      </main>
    </div>
  );
}
