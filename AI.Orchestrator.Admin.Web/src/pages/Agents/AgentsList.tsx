import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { agentsApi } from '@/services/api';
import { Bot, Plus, Edit, Trash2, AlertCircle } from 'lucide-react';
import type { Agent } from '@/types';

const AgentsList: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  const fetchAgents = async () => {
    try {
      setLoading(true);
      const response = await agentsApi.getAll();
      setAgents(response.data);
      setError('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load agents');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAgents();
  }, []);

  const handleDelete = async (id: string) => {
    try {
      await agentsApi.delete(id);
      setAgents(agents.filter((a) => a.id !== id));
      setDeleteConfirm(null);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete agent');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Agents</h1>
          <p className="mt-2 text-gray-600">
            Manage your AI agents and their configurations
          </p>
        </div>
        <Link to="/agents/new" className="btn-primary flex items-center">
          <Plus className="h-5 w-5 mr-2" />
          Create Agent
        </Link>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start">
          <AlertCircle className="h-5 w-5 text-red-600 mt-0.5 mr-2 flex-shrink-0" />
          <span className="text-sm text-red-800">{error}</span>
        </div>
      )}

      {/* Agents List */}
      {agents.length === 0 ? (
        <div className="card text-center py-12">
          <Bot className="h-16 w-16 mx-auto mb-4 text-gray-400" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            No agents yet
          </h3>
          <p className="text-gray-600 mb-6">
            Get started by creating your first AI agent
          </p>
          <Link to="/agents/new" className="btn-primary inline-flex items-center">
            <Plus className="h-5 w-5 mr-2" />
            Create Agent
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {agents.map((agent) => (
            <div key={agent.id} className="card hover:shadow-md transition-shadow">
              <div className="flex items-start justify-between mb-4">
                <div className="flex items-start flex-1">
                  <div className="p-2 bg-primary-100 rounded-lg mr-4">
                    <Bot className="h-6 w-6 text-primary-600" />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-lg font-semibold text-gray-900">
                      {agent.name}
                    </h3>
                    {agent.description && (
                      <p className="mt-1 text-sm text-gray-600">
                        {agent.description}
                      </p>
                    )}
                  </div>
                </div>
                <div className="flex items-center space-x-2">
                  {agent.isActive ? (
                    <span className="px-2 py-1 text-xs font-medium bg-green-100 text-green-800 rounded">
                      Active
                    </span>
                  ) : (
                    <span className="px-2 py-1 text-xs font-medium bg-gray-200 text-gray-800 rounded">
                      Inactive
                    </span>
                  )}
                </div>
              </div>

              <div className="space-y-2 mb-4">
                <div className="flex items-center text-sm text-gray-600">
                  <span className="font-medium mr-2">Created by:</span>
                  {agent.createdByUsername}
                </div>
                <div className="flex items-center text-sm text-gray-600">
                  <span className="font-medium mr-2">Plugins:</span>
                  {agent.pluginConfigurations.length} configured
                </div>
                <div className="flex items-center text-sm text-gray-600">
                  <span className="font-medium mr-2">Created:</span>
                  {new Date(agent.createdAt).toLocaleDateString()}
                </div>
              </div>

              <div className="flex items-center justify-between pt-4 border-t border-gray-200">
                <Link
                  to={`/agents/${agent.id}`}
                  className="text-primary-600 hover:text-primary-700 font-medium text-sm"
                >
                  View Details
                </Link>
                <div className="flex items-center space-x-2">
                  <Link
                    to={`/agents/${agent.id}/edit`}
                    className="p-2 text-gray-600 hover:text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
                    title="Edit agent"
                  >
                    <Edit className="h-4 w-4" />
                  </Link>
                  {deleteConfirm === agent.id ? (
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => handleDelete(agent.id)}
                        className="px-3 py-1 text-xs font-medium bg-red-600 text-white rounded hover:bg-red-700"
                      >
                        Confirm
                      </button>
                      <button
                        onClick={() => setDeleteConfirm(null)}
                        className="px-3 py-1 text-xs font-medium bg-gray-200 text-gray-900 rounded hover:bg-gray-300"
                      >
                        Cancel
                      </button>
                    </div>
                  ) : (
                    <button
                      onClick={() => setDeleteConfirm(agent.id)}
                      className="p-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                      title="Delete agent"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default AgentsList;
