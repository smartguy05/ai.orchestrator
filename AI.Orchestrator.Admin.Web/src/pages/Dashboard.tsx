import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { agentsApi, usersApi } from '@/services/api';
import { Bot, Users, Activity, Plus } from 'lucide-react';
import type { Agent, User } from '@/types';

const Dashboard: React.FC = () => {
  const { user, isAdmin } = useAuth();
  const [agents, setAgents] = useState<Agent[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [agentsResponse, usersResponse] = await Promise.allSettled([
          agentsApi.getAll(),
          isAdmin ? usersApi.getAll() : Promise.resolve({ data: [] }),
        ]);

        if (agentsResponse.status === 'fulfilled') {
          setAgents(agentsResponse.value.data);
        }

        if (usersResponse.status === 'fulfilled') {
          setUsers(usersResponse.value.data);
        }
      } catch (error) {
        console.error('Failed to fetch dashboard data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [isAdmin]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  const activeAgents = agents.filter((a) => a.isActive).length;
  const myAgents = agents.filter((a) => a.createdByUsername === user?.username);
  const activeUsers = users.filter((u) => u.isActive).length;

  return (
    <div>
      {/* Welcome Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">
          Welcome back, {user?.username}!
        </h1>
        <p className="mt-2 text-gray-600">
          Here's an overview of your AI Orchestrator system.
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
        {/* Total Agents Card */}
        <div className="card">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Agents</p>
              <p className="mt-2 text-3xl font-bold text-gray-900">
                {agents.length}
              </p>
              <p className="mt-1 text-sm text-gray-500">
                {activeAgents} active
              </p>
            </div>
            <div className="p-3 bg-primary-100 rounded-lg">
              <Bot className="h-8 w-8 text-primary-600" />
            </div>
          </div>
        </div>

        {/* My Agents Card */}
        <div className="card">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">My Agents</p>
              <p className="mt-2 text-3xl font-bold text-gray-900">
                {myAgents.length}
              </p>
              <p className="mt-1 text-sm text-gray-500">
                Created by you
              </p>
            </div>
            <div className="p-3 bg-green-100 rounded-lg">
              <Activity className="h-8 w-8 text-green-600" />
            </div>
          </div>
        </div>

        {/* Users Card (Admin only) */}
        {isAdmin && (
          <div className="card">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Users</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">
                  {users.length}
                </p>
                <p className="mt-1 text-sm text-gray-500">
                  {activeUsers} active
                </p>
              </div>
              <div className="p-3 bg-purple-100 rounded-lg">
                <Users className="h-8 w-8 text-purple-600" />
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Quick Actions */}
      <div className="card mb-8">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link
            to="/agents/new"
            className="flex items-center p-4 bg-primary-50 border border-primary-200 rounded-lg hover:bg-primary-100 transition-colors"
          >
            <Plus className="h-6 w-6 text-primary-600 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Create New Agent</p>
              <p className="text-sm text-gray-600">
                Set up a new AI agent with custom configuration
              </p>
            </div>
          </Link>

          <Link
            to="/agents"
            className="flex items-center p-4 bg-gray-50 border border-gray-200 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <Bot className="h-6 w-6 text-gray-600 mr-3" />
            <div>
              <p className="font-medium text-gray-900">View All Agents</p>
              <p className="text-sm text-gray-600">
                Manage and configure your existing agents
              </p>
            </div>
          </Link>

          {isAdmin && (
            <>
              <Link
                to="/users"
                className="flex items-center p-4 bg-gray-50 border border-gray-200 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <Users className="h-6 w-6 text-gray-600 mr-3" />
                <div>
                  <p className="font-medium text-gray-900">Manage Users</p>
                  <p className="text-sm text-gray-600">
                    View and manage user accounts and roles
                  </p>
                </div>
              </Link>

              <Link
                to="/audit-logs"
                className="flex items-center p-4 bg-gray-50 border border-gray-200 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <Activity className="h-6 w-6 text-gray-600 mr-3" />
                <div>
                  <p className="font-medium text-gray-900">View Audit Logs</p>
                  <p className="text-sm text-gray-600">
                    Monitor system activity and security events
                  </p>
                </div>
              </Link>
            </>
          )}
        </div>
      </div>

      {/* Recent Agents */}
      <div className="card">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-bold text-gray-900">Recent Agents</h2>
          <Link
            to="/agents"
            className="text-sm font-medium text-primary-600 hover:text-primary-700"
          >
            View all
          </Link>
        </div>

        {agents.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <Bot className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <p>No agents created yet.</p>
            <Link
              to="/agents/new"
              className="mt-2 inline-block text-primary-600 hover:text-primary-700"
            >
              Create your first agent
            </Link>
          </div>
        ) : (
          <div className="space-y-3">
            {agents.slice(0, 5).map((agent) => (
              <Link
                key={agent.id}
                to={`/agents/${agent.id}`}
                className="block p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center">
                    <Bot className="h-5 w-5 text-gray-600 mr-3" />
                    <div>
                      <p className="font-medium text-gray-900">{agent.name}</p>
                      {agent.description && (
                        <p className="text-sm text-gray-600 mt-0.5">
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
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
