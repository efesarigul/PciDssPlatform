import { NavLink, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import Login from "./pages/Login";
import Scope from "./pages/Scope";
import SurveyWizard from "./pages/SurveyWizard";
import Evidence from "./pages/Evidence";

function Protected({ children }) {
  return localStorage.getItem("token") ? children : <Navigate to="/login" replace />;
}

function Shell({ children }) {
  const nav = useNavigate();
  const org = localStorage.getItem("org");
  const logout = () => { localStorage.clear(); nav("/login"); };
  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">PCI<span>DSS</span> Ön Değerlendirme</div>
        <div className="org">{org}</div>
        <nav>
          <NavLink to="/scope">1 · Kapsam Belirleme</NavLink>
          <NavLink to="/survey">2 · Beyan Anketi</NavLink>
          <NavLink to="/evidence">3 · Kanıt Yükleme</NavLink>
          <span className="disabled" title="Faz 3">4 · Gap Analizi</span>
        </nav>
        <button className="ghost" onClick={logout}>Çıkış</button>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/scope" element={<Protected><Shell><Scope /></Shell></Protected>} />
      <Route path="/survey"element={<Protected><Shell><SurveyWizard /></Shell></Protected>} />
      <Route path="/evidence" element={<Protected><Shell><Evidence /></Shell></Protected>} />
      <Route path="*" element={<Navigate to="/survey" replace />} />
    </Routes>
  );
}
