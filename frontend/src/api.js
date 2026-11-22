import axios from "axios";

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL ?? "https://localhost:7001/api" });

api.interceptors.request.use((cfg) => {
  const token = localStorage.getItem("token");
  if (token) cfg.headers.Authorization = `Bearer ${token}`;
  return cfg;
});

api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem("token");
      window.location.href = "/login";
    }
    if (!err.response) {
      // Backend kapalı veya henüz açılıyor (ilk derleme birkaç saniye sürebilir)
      return Promise.reject(new Error("Sunucuya ulaşılamadı. Backend açılıyor olabilir; birkaç saniye sonra sayfayı yenileyin."));
    }
    return Promise.reject(new Error(err.response.data?.error ?? `Beklenmeyen bir hata oluştu (HTTP ${err.response.status}).`));
  }
);

export const authApi = {
  login: (email, password) => api.post("/auth/login", { email, password }).then((r) => r.data),
  register: (dto) => api.post("/auth/register", dto).then((r) => r.data),
  demo: () => api.post("/auth/demo").then((r) => r.data),
  demoAuditor: () => api.post("/auth/demo-auditor").then((r) => r.data),
};

// Kurum tarafı: denetçilere verilen erişimler
export const auditorsApi = {
  list: () => api.get("/auditors").then((r) => r.data),
  grant: (email) => api.post("/auditors", { email }).then((r) => r.data),
  revoke: (id) => api.delete(`/auditors/${id}`),
};

// Denetçi paneli
export const auditApi = {
  orgs: () => api.get("/audit/orgs").then((r) => r.data),
  org: (orgId) => api.get(`/audit/orgs/${orgId}`).then((r) => r.data),
  review: (orgId, questionId, decision, note) =>
    api.put(`/audit/orgs/${orgId}/reviews/${questionId}`, { decision, note }).then((r) => r.data),
  file: (orgId, id) => api.get(`/audit/orgs/${orgId}/files/${id}`, { responseType: "blob" }).then((r) => r.data),
};

export const gapApi = {
  get: () => api.get("/gap").then((r) => r.data),
};

export const scopeApi = {
  get: () => api.get("/scope").then((r) => r.data || null),
  save: (dto) => api.put("/scope", dto).then((r) => r.data),
};

export const surveyApi = {
  questions: () => api.get("/survey/questions").then((r) => r.data),
  save: (answers) => api.post("/survey/answers", { answers }),
  progress: () => api.get("/survey/progress").then((r) => r.data),
};

export const filesApi = {
  list: () => api.get("/files").then((r) => r.data),
  upload: (file, requirementCode, onProgress) => {
    const form = new FormData();
    form.append("file", file);
    if (requirementCode) form.append("requirementCode", requirementCode);
    return api
      .post("/files", form, {
        onUploadProgress: (e) => onProgress?.(Math.round((e.loaded * 100) / (e.total || 1))),
      })
      .then((r) => r.data);
  },
};
