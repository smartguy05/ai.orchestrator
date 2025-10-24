# AI Orchestrator Admin Web

Modern React web application for managing AI Orchestrator agents, users, and configurations.

## Features

### Authentication & Authorization
- **User Registration & Login** - Secure JWT-based authentication
- **Refresh Token Support** - Stay logged in with automatic token refresh
- **Role-Based Access Control** - Admin, AgentManager, User, and ReadOnly roles
- **Protected Routes** - Automatic redirection for unauthenticated users

### Agent Management
- **Agent List View** - Browse all agents with search and filter
- **Create/Edit Agents** - Easy-to-use forms for agent configuration
- **Agent Details** - View agent information and plugin configurations
- **Status Management** - Activate/deactivate agents

### Admin Features
- **User Management** - View all users, roles, and activity
- **Audit Logs Viewer** - Monitor system activity and security events
  - Filter by user, action, or date range
  - View failed security events
  - Search by action type
- **Security Monitoring** - Track failed logins and unauthorized access

### User Experience
- **Responsive Design** - Works on desktop, tablet, and mobile
- **Real-time Updates** - Instant feedback on actions
- **Error Handling** - Clear error messages and validation
- **Loading States** - Smooth loading indicators

## Tech Stack

- **React 18** - Modern UI library with hooks
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool and dev server
- **React Router** - Client-side routing
- **Axios** - HTTP client with interceptors
- **React Hook Form** - Performant form validation
- **Tailwind CSS** - Utility-first styling
- **Lucide React** - Beautiful icon library
- **date-fns** - Date formatting and manipulation

## Getting Started

### Prerequisites

- Node.js 18+ and npm
- AI Orchestrator backend running on `http://localhost:5178`

### Installation

1. Install dependencies:
```bash
npm install
```

2. Copy environment variables (optional):
```bash
cp .env.example .env
```

3. Start development server:
```bash
npm run dev
```

The app will open at `http://localhost:3000`

### Building for Production

```bash
npm run build
```

This creates an optimized production build in the `dist/` directory.

Preview the production build:
```bash
npm run preview
```

## Project Structure

```
AI.Orchestrator.Admin.Web/
├── src/
│   ├── components/          # Reusable components
│   │   ├── Layout.tsx       # Main layout with navigation
│   │   └── ProtectedRoute.tsx  # Route authentication wrapper
│   ├── contexts/            # React contexts
│   │   └── AuthContext.tsx  # Authentication state management
│   ├── pages/               # Page components
│   │   ├── Login.tsx        # Login page
│   │   ├── Register.tsx     # Registration page
│   │   ├── Dashboard.tsx    # Home dashboard
│   │   ├── Agents/          # Agent management pages
│   │   │   ├── AgentsList.tsx
│   │   │   ├── AgentDetail.tsx
│   │   │   └── AgentForm.tsx
│   │   ├── AuditLogs.tsx    # Admin audit logs (admin only)
│   │   └── Users.tsx        # User management (admin only)
│   ├── services/            # API services
│   │   └── api.ts           # Axios configuration and API calls
│   ├── types/               # TypeScript type definitions
│   │   └── index.ts         # All API types
│   ├── App.tsx              # Main app component with routing
│   ├── main.tsx             # Application entry point
│   └── index.css            # Global styles and Tailwind
├── index.html               # HTML template
├── vite.config.ts           # Vite configuration
├── tsconfig.json            # TypeScript configuration
├── tailwind.config.js       # Tailwind CSS configuration
└── package.json             # Project dependencies
```

## API Integration

The app connects to the AI Orchestrator backend API using a proxy configuration in Vite:

```typescript
// vite.config.ts
server: {
  port: 3000,
  proxy: {
    '/api': {
      target: 'http://localhost:5178',
      changeOrigin: true,
    },
  },
}
```

All API calls to `/api/*` are automatically forwarded to the backend.

### API Endpoints Used

**Authentication:**
- POST `/api/auth/login` - User login
- POST `/api/auth/register` - User registration
- POST `/api/auth/refresh` - Refresh access token
- POST `/api/auth/revoke` - Revoke refresh token

**Agents:**
- GET `/api/agents` - List all agents
- GET `/api/agents/:id` - Get agent details
- POST `/api/agents` - Create new agent
- PUT `/api/agents/:id` - Update agent
- DELETE `/api/agents/:id` - Delete agent

**Users (Admin only):**
- GET `/api/users` - List all users
- GET `/api/users/:id` - Get user details
- POST `/api/users/:id/roles/:roleName` - Assign role
- DELETE `/api/users/:id/roles/:roleName` - Remove role

**Plugin Configurations:**
- GET `/api/pluginconfigs/agent/:agentId` - Get agent configs
- GET `/api/pluginconfigs/user/:userId` - Get user configs
- GET `/api/pluginconfigs/global` - Get global configs
- POST `/api/pluginconfigs` - Create config
- PUT `/api/pluginconfigs/:id` - Update config
- DELETE `/api/pluginconfigs/:id` - Delete config

**Audit Logs (Admin only):**
- GET `/api/auditlogs/user/:userId` - Get user audit logs
- GET `/api/auditlogs/entity/:type/:id` - Get entity audit logs
- GET `/api/auditlogs/security/failed` - Get failed security events
- GET `/api/auditlogs/search/action/:action` - Search by action

## Authentication Flow

1. **Login**: User submits credentials → receives JWT + refresh token
2. **Token Storage**: Tokens stored in localStorage
3. **Auto Refresh**: Axios interceptor catches 401 errors → refreshes token
4. **Logout**: Revokes refresh token → clears localStorage

### Token Management

```typescript
// Request interceptor adds token to all requests
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor handles token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Attempt token refresh
      const refreshToken = localStorage.getItem('refreshToken');
      const response = await authApi.refreshToken({ refreshToken });
      // Retry original request with new token
    }
  }
);
```

## Development

### Available Scripts

```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run preview      # Preview production build
npm run lint         # Run ESLint
npm run type-check   # Run TypeScript compiler check
```

### Code Style

The project uses:
- **ESLint** for code linting
- **TypeScript** for type checking
- **Prettier** (recommended) for code formatting

### Adding New Pages

1. Create page component in `src/pages/`
2. Add route in `src/App.tsx`
3. Add navigation link in `src/components/Layout.tsx` (if needed)

Example:
```typescript
// src/pages/MyPage.tsx
const MyPage: React.FC = () => {
  return <div>My Page Content</div>;
};

// src/App.tsx
<Route
  path="/my-page"
  element={
    <ProtectedRoute>
      <Layout>
        <MyPage />
      </Layout>
    </ProtectedRoute>
  }
/>
```

## Deployment

### Environment Variables

For production deployment, configure:

```bash
VITE_API_URL=https://your-api-domain.com
```

### Build and Deploy

1. Build the application:
```bash
npm run build
```

2. The `dist/` folder contains static files ready for deployment

3. Deploy to any static hosting service:
   - **Vercel**: `vercel deploy`
   - **Netlify**: Drag & drop `dist/` folder
   - **AWS S3**: `aws s3 sync dist/ s3://your-bucket`
   - **Azure Static Web Apps**: Follow Azure deployment guide
   - **GitHub Pages**: Use GitHub Actions

### Nginx Configuration

If deploying behind Nginx, use this configuration:

```nginx
server {
  listen 80;
  server_name your-domain.com;
  root /var/www/ai-orchestrator-admin;
  index index.html;

  # React Router support
  location / {
    try_files $uri $uri/ /index.html;
  }

  # API proxy
  location /api/ {
    proxy_pass http://localhost:5178;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection 'upgrade';
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
  }
}
```

## Troubleshooting

### Cannot connect to API

- Verify backend is running on `http://localhost:5178`
- Check Vite proxy configuration in `vite.config.ts`
- Check browser console for CORS errors

### Authentication not working

- Clear localStorage: `localStorage.clear()`
- Verify JWT secret matches backend configuration
- Check token expiration times

### Build errors

- Delete `node_modules/` and reinstall: `npm install`
- Clear Vite cache: `rm -rf node_modules/.vite`
- Check Node.js version: `node --version` (should be 18+)

## Contributing

1. Create a feature branch
2. Make your changes
3. Run linter: `npm run lint`
4. Run type check: `npm run type-check`
5. Submit pull request

## License

This project is part of the AI Orchestrator system.
