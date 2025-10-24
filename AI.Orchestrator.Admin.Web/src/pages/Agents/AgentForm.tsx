import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { agentsApi } from '@/services/api';
import { AlertCircle, Save, ArrowLeft } from 'lucide-react';
import type { CreateAgentRequest, UpdateAgentRequest, Agent } from '@/types';

type FormData = CreateAgentRequest | UpdateAgentRequest;

const AgentForm: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEdit = !!id;

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [agent, setAgent] = useState<Agent | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({
    defaultValues: {
      name: '',
      description: '',
      isActive: true,
    },
  });

  // Load agent data if editing
  useEffect(() => {
    if (isEdit && id) {
      const fetchAgent = async () => {
        try {
          setLoading(true);
          const response = await agentsApi.getById(id);
          setAgent(response.data);
          reset({
            name: response.data.name,
            description: response.data.description || '',
            isActive: response.data.isActive,
          });
        } catch (err: any) {
          setError(err.response?.data?.message || 'Failed to load agent');
        } finally {
          setLoading(false);
        }
      };
      fetchAgent();
    }
  }, [id, isEdit, reset]);

  const onSubmit = async (data: FormData) => {
    setError('');
    setLoading(true);

    try {
      if (isEdit && id) {
        await agentsApi.update(id, data as UpdateAgentRequest);
      } else {
        await agentsApi.create(data as CreateAgentRequest);
      }
      navigate('/agents');
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          `Failed to ${isEdit ? 'update' : 'create'} agent`
      );
    } finally {
      setLoading(false);
    }
  };

  if (isEdit && loading && !agent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
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
        <h1 className="text-3xl font-bold text-gray-900">
          {isEdit ? 'Edit Agent' : 'Create New Agent'}
        </h1>
        <p className="mt-2 text-gray-600">
          {isEdit
            ? 'Update the agent details and configuration'
            : 'Set up a new AI agent with custom settings'}
        </p>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start">
          <AlertCircle className="h-5 w-5 text-red-600 mt-0.5 mr-2 flex-shrink-0" />
          <span className="text-sm text-red-800">{error}</span>
        </div>
      )}

      {/* Form */}
      <div className="card max-w-2xl">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          {/* Name Field */}
          <div>
            <label htmlFor="name" className="label">
              Agent Name *
            </label>
            <input
              id="name"
              type="text"
              className={`input ${errors.name ? 'border-red-500' : ''}`}
              placeholder="e.g., Customer Support Agent"
              {...register('name', {
                required: 'Agent name is required',
                minLength: {
                  value: 3,
                  message: 'Name must be at least 3 characters',
                },
              })}
            />
            {errors.name && (
              <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>
            )}
            <p className="mt-1 text-sm text-gray-500">
              A descriptive name for your agent
            </p>
          </div>

          {/* Description Field */}
          <div>
            <label htmlFor="description" className="label">
              Description
            </label>
            <textarea
              id="description"
              rows={4}
              className={`input ${errors.description ? 'border-red-500' : ''}`}
              placeholder="Describe what this agent does and its purpose..."
              {...register('description')}
            />
            {errors.description && (
              <p className="mt-1 text-sm text-red-600">
                {errors.description.message}
              </p>
            )}
            <p className="mt-1 text-sm text-gray-500">
              Optional description to help identify the agent's purpose
            </p>
          </div>

          {/* Active Status Field */}
          <div>
            <div className="flex items-center">
              <input
                id="isActive"
                type="checkbox"
                className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                {...register('isActive')}
              />
              <label
                htmlFor="isActive"
                className="ml-2 block text-sm font-medium text-gray-700"
              >
                Active
              </label>
            </div>
            <p className="mt-1 text-sm text-gray-500 ml-6">
              Inactive agents will not process requests
            </p>
          </div>

          {/* Submit Buttons */}
          <div className="flex items-center justify-end space-x-4 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={() => navigate('/agents')}
              className="btn-secondary"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="btn-primary flex items-center disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Save className="h-5 w-5 mr-2" />
              {loading
                ? isEdit
                  ? 'Updating...'
                  : 'Creating...'
                : isEdit
                ? 'Update Agent'
                : 'Create Agent'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AgentForm;
