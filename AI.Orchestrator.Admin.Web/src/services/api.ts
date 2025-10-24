import axios from 'axios';
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RefreshTokenRequest,
  User,
  Agent,
  CreateAgentRequest,
  UpdateAgentRequest,
  PluginConfiguration,
  CreatePluginConfigRequest,
  UpdatePluginConfigRequest,
  AuditLog,
} from '@/types';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor to handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If error is 401 and we haven't tried to refresh yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        if (refreshToken) {
          const response = await authApi.refreshToken({ refreshToken });

          localStorage.setItem('token', response.data.token);
          localStorage.setItem('refreshToken', response.data.refreshToken);

          // Retry original request with new token
          originalRequest.headers.Authorization = `Bearer ${response.data.token}`;
          return api(originalRequest);
        }
      } catch (refreshError) {
        // Refresh failed, redirect to login
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  login: (data: LoginRequest) =>
    api.post<LoginResponse>('/auth/login', data),

  register: (data: RegisterRequest) =>
    api.post<User>('/auth/register', data),

  refreshToken: (data: RefreshTokenRequest) =>
    api.post<LoginResponse>('/auth/refresh', data),

  revokeToken: (refreshToken: string) =>
    api.post('/auth/revoke', { refreshToken }),
};

// Users API - Note: route is /user (singular)
export const usersApi = {
  getAll: () =>
    api.get<User[]>('/user'),

  getById: (id: string) =>
    api.get<User>(`/user/${id}`),

  update: (id: string, data: { email?: string; isActive?: boolean }) =>
    api.put<User>(`/user/${id}`, data),

  delete: (id: string) =>
    api.delete(`/user/${id}`),

  assignRole: (userId: string, roleId: number) =>
    api.post(`/user/${userId}/roles/${roleId}`),

  removeRole: (userId: string, roleId: number) =>
    api.delete(`/user/${userId}/roles/${roleId}`),

  getUsersByRole: (roleId: number) =>
    api.get<User[]>(`/user/by-role/${roleId}`),
};

// Agents API - Note: route is /agent (singular)
export const agentsApi = {
  getAll: () =>
    api.get<Agent[]>('/agent'),

  getById: (id: string) =>
    api.get<Agent>(`/agent/${id}`),

  create: (data: CreateAgentRequest) =>
    api.post<Agent>('/agent', data),

  update: (id: string, data: UpdateAgentRequest) =>
    api.put<Agent>(`/agent/${id}`, data),

  delete: (id: string) =>
    api.delete(`/agent/${id}`),

  addTool: (id: string, toolName: string) =>
    api.post(`/agent/${id}/tools/${toolName}`),

  removeTool: (id: string, toolName: string) =>
    api.delete(`/agent/${id}/tools/${toolName}`),
};

// Plugin Configurations API - Note: route is /plugin-configs (plural with dash)
export const pluginConfigsApi = {
  getByAgent: (agentId: string) =>
    api.get<PluginConfiguration[]>(`/plugin-configs/agent/${agentId}`),

  getByAgentAndPlugin: (agentId: string, pluginName: string) =>
    api.get<PluginConfiguration>(`/plugin-configs/agent/${agentId}/${pluginName}`),

  getDefaults: () =>
    api.get<PluginConfiguration[]>('/plugin-configs/defaults'),

  getById: (id: string) =>
    api.get<PluginConfiguration>(`/plugin-configs/${id}`),

  create: (data: CreatePluginConfigRequest) =>
    api.post<PluginConfiguration>('/plugin-configs', data),

  update: (id: string, data: UpdatePluginConfigRequest) =>
    api.put<PluginConfiguration>(`/plugin-configs/${id}`, data),

  delete: (id: string) =>
    api.delete(`/plugin-configs/${id}`),
};

// Audit Logs API (Admin only)
export const auditLogsApi = {
  getByUser: (userId: string, params?: {
    startDate?: string;
    endDate?: string;
    limit?: number;
  }) =>
    api.get<AuditLog[]>(`/auditlogs/user/${userId}`, { params }),

  getByEntity: (entityType: string, entityId: string, limit?: number) =>
    api.get<AuditLog[]>(`/auditlogs/entity/${entityType}/${entityId}`, {
      params: { limit },
    }),

  getFailedSecurity: (params?: {
    startDate?: string;
    limit?: number;
  }) =>
    api.get<AuditLog[]>('/auditlogs/security/failed', { params }),

  searchByAction: (action: string, params?: {
    startDate?: string;
    endDate?: string;
    limit?: number;
  }) =>
    api.get<AuditLog[]>(`/auditlogs/search/action/${action}`, { params }),
};

export default api;
