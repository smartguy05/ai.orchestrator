// Auth types
export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  username: string;
  email: string;
  roles: string[];
  expiresAt: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

// User types
export interface User {
  id: string;
  username: string;
  email: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string;
}

// Agent types
export interface Agent {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdByUserId: string;
  createdByUsername: string;
  createdAt: string;
  updatedAt: string;
  pluginConfigurations: PluginConfiguration[];
}

export interface CreateAgentRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface UpdateAgentRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

// Plugin Configuration types
export interface PluginConfiguration {
  id: string;
  pluginName: string;
  configurationJson: string;
  scope: 'Global' | 'Agent' | 'User';
  agentId?: string;
  userId?: string;
  createdByUserId: string;
  createdByUsername: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePluginConfigRequest {
  pluginName: string;
  configurationJson: string;
  scope: 'Global' | 'Agent' | 'User';
  agentId?: string;
}

export interface UpdatePluginConfigRequest {
  configurationJson: string;
}

// Audit Log types
export interface AuditLog {
  id: string;
  userId?: string;
  username: string;
  action: string;
  entityType: string;
  entityId?: string;
  outcome: string;
  ipAddress?: string;
  userAgent?: string;
  details?: string;
  timestamp: string;
}

// Role types
export type RoleName = 'Admin' | 'AgentManager' | 'User' | 'ReadOnly';

export interface Role {
  id: string;
  name: RoleName;
  description: string;
}

// API Response types
export interface ApiError {
  message: string;
  error?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
