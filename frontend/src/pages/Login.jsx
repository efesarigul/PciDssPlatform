import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api";

export default function Login() {
  const nav = useNavigate();
  const [mode, setMode] = useState("login");
  const [form, setForm] = useState({ fullName: "", email: "", password: "", organizationName: "" });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  const submit = async (e) => {
    e.preventDefault();
    setError(""); setBusy(true);
    try {
      const res = mode === "login" ? await authApi.login(form.email, form.password) : await authApi.register(form);
      localStorage.setItem("token", res.token);
      localStorage.setItem("org", res.organizationName);
      nav("/survey");
    } catch (err) { setError(err.message); } finally { setBusy(false); }
  };

  return (
    <div className="auth-page">
      <form className="card auth" onSubmit={submit}>
        <h1>{mode === "login" ? "Giriş Yap" : "Kurum Kaydı"}</h1>
        <p className="muted">PCI DSS uyum ön değerlendirme platformu</p>
        {mode === "register" && (
          <>
            <label>Ad Soyad<input required value={form.fullName} onChange={set("fullName")} /></label>
            <label>Kurum Adı<input required value={form.organizationName} onChange={set("organizationName")} /></label>
          </>
        )}
        <label>E-posta<input type="email" required value={form.email} onChange={set("email")} /></label>
        <label>Parola<input type="password" required minLength={12} value={form.password} onChange={set("password")} /></label>
        {error && <div className="error">{error}</div>}
        <button disabled={busy}>{busy ? "..." : mode === "login" ? "Giriş" : "Kayıt Ol"}</button>
        <button type="button" className="link" onClick={() => setMode(mode === "login" ? "register" : "login")}>
          {mode === "login" ? "Hesabın yok mu? Kurum kaydı oluştur" : "Zaten hesabın var mı? Giriş yap"}
        </button>
      </form>
    </div>
  );
}
