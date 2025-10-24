import React, { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { agentsApi, pluginConfigsApi } from '@/services/api';
import {
  ArrowLeft,
  Edit,
  Bot,
  Settings,
  Activity,
  AlertCircle,
} from 'lucide-react';
import type { Agent, PluginConfiguration } from '@/types';

const AgentDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [agent, setAgent] = useState<Agent | null>(null);
  const [pluginConfigs, setPluginConfigs] = useState<PluginConfiguration[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    if (!id) return;

    const fetchData = async () => {
      try {
        setLoading(true);
        const [agentResponse, pluginsResponse] = await Promise.all([
          agentsApi.getById(id),
          pluginConfigsApi.getByAgent(id),
        ]);

        setAgent(agentResponse.data);
        setPluginConfigs(pluginsResponse.data);
        setError('');
      } catch (err: any) {
        setError(err.response?.data?.message || 'Failed to load agent details');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (error || !agent) {
    return (
      <div className="card text-center py-12">
        <AlertCircle className="h-16 w-16 mx-auto mb-4 text-red-400" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {error || 'Agent not found'}
        </h3>
        <button
          onClick={() => navigate('/agents')}
          className="mt-4 text-primary-600 hover:text-primary-700"
        >
          Back to Agents
        </button>
      </div>
    );
  }

  return (
    <div>
      {/* Header */}
      <div className="mb-6">
        <button
          onClick={() => navigate('/agents')}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
        >
          <ArrowLeft className="h-5 w-5 mr-1" />
          Back to Agents
        </button>

        <div className="flex items-start justify-between">
          <div className="flex items-start">
            <div className="p-3 bg-primary-100 rounded-lg mr-4">
              <Bot className="h-8 w-8 text-primary-600" />
            </div>
            <div>
              <h1 className="text-3xl font-bold text-gray-900">{agent.name}</h1>
              {agent.description && (
                <p className="mt-2 text-gray-600">{agent.description}</p>
              )}
            </div>
          </div>
          <Link
            to={`/agents/${id}/edit`}
            className="btn-primary flex items-center"
          >
            <Edit className="h-5 w-5 mr-2" />
            Edit Agent
          </Link>
        </div>
      </div>

      {/* Agent Details Card */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        <div className="card">
          <div className="flex items-center mb-2">
            <Activity className="h-5 w-5 text-gray-600 mr-2" />
            <span className="text-sm font-medium text-gray-600">Status</span>
          </div>
          {agent.isActive ? (
            <span className="inline-flex items-center px-3 py-1 text-sm font-medium bg-green-100 text-green-800 rounded-full">
              Active
            </span>
          ) : (
            <span className="inline-flex items-center px-3 py-1 text-sm font-medium bg-gray-200 text-gray-800 rounded-full">
              Inactive
            </span>
          )}
        </div>

        <div className="card">
          <div className="flex items-center mb-2">
            <Settings className="h-5 w-5 text-gray-600 mr-2" />
            <span className="text-sm font-medium text-gray-600">
              Plugin Configurations
            </span>
          </div>
          <p className="text-2xl font-bold text-gray-900">
            {pluginConfigs.length}
          </p>
        </div>

        <div className="card">
          <div className="flex items-center mb-2">
            <Bot className="h-5 w-5 text-gray-600 mr-2" />
            <span className="text-sm font-medium text-gray-600">Created By</span>
          </div>
          <p className="font-medium text-gray-900">{agent.createdByUsername}</p>
          <p className="text-sm text-gray-600">
            {new Date(agent.createdAt).toLocaleDateString()}
          </p>
        </div>
      </div>

      {/* Plugin Configurations */}
      <div className="card">
        <h2 className="text-xl font-bold text-gray-900 mb-4">
          Plugin Configurations
        </h2>

        {pluginConfigs.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <Settings className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <p>No plugin configurations yet.</p>
            <p className="text-sm mt-1">
              Plugin configurations allow you to customize agent behavior.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {pluginConfigs.map((config) => (
              <div
                key={config.id}
                className="p-4 bg-gray-50 rounded-lg border border-gray-200"
              >
                <div className="flex items-start justify-between mb-2">
                  <h3 className="font-semibold text-gray-900">
                    {config.pluginName}
                  </h3>
                  <span className="px-2 py-1 text-xs font-medium bg-primary-100 text-primary-800 rounded">
                    {config.scope}
                  </span>
                </div>

                <div className="mb-3">
                  <p className="text-xs text-gray-500 mb-1">Configuration:</p>
                  <pre className="text-xs bg-gray-900 text-gray-100 p-3 rounded overflow-x-auto">
                    {JSON.stringify(JSON.parse(config.configurationJson), null, 2)}
                  </pre>
                </div>

                <div className="flex items-center justify-between text-xs text-gray-600">
                  <span>Created by {config.createdByUsername}</span>
                  <span>{new Date(config.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Metadata */}
      <div className="card mt-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Metadata</h2>
        <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <dt className="text-sm font-medium text-gray-600">Agent ID</dt>
            <dd className="mt-1 text-sm text-gray-900 font-mono">{agent.id}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-600">Created By</dt>
            <dd className="mt-1 text-sm text-gray-900">
              {agent.createdByUsername} (ID: {agent.createdByUserId})
            </dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-600">Created At</dt>
            <dd className="mt-1 text-sm text-gray-900">
              {new Date(agent.createdAt).toLocaleString()}
            </dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-600">Updated At</dt>
            <dd className="mt-1 text-sm text-gray-900">
              {new Date(agent.updatedAt).toLocaleString()}
            </dd>
          </div>
        </dl>
      </div>
    </div>
  );
};

export default AgentDetail;
