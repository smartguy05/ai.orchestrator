import React, { useEffect, useState } from 'react';
import { auditLogsApi } from '@/services/api';
import {
  FileText,
  AlertCircle,
  Filter,
  Calendar,
  User,
  Activity,
} from 'lucide-react';
import { format } from 'date-fns';
import type { AuditLog } from '@/types';

type FilterType = 'all' | 'security' | 'user' | 'action';

const AuditLogs: React.FC = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [filterType, setFilterType] = useState<FilterType>('all');
  const [filterValue, setFilterValue] = useState<string>('');
  const [startDate, setStartDate] = useState<string>('');
  const [limit, setLimit] = useState<number>(100);

  const fetchLogs = async () => {
    try {
      setLoading(true);
      setError('');

      let response;
      const params = {
        startDate: startDate || undefined,
        limit,
      };

      switch (filterType) {
        case 'security':
          response = await auditLogsApi.getFailedSecurity(params);
          break;
        case 'user':
          if (filterValue) {
            response = await auditLogsApi.getByUser(filterValue, params);
          } else {
            setError('Please enter a user ID for user filter');
            setLoading(false);
            return;
          }
          break;
        case 'action':
          if (filterValue) {
            response = await auditLogsApi.searchByAction(filterValue, params);
          } else {
            setError('Please enter an action name for action filter');
            setLoading(false);
            return;
          }
          break;
        default:
          // For 'all', we'll fetch failed security events as a starting point
          response = await auditLogsApi.getFailedSecurity({ limit: 1000 });
          break;
      }

      setLogs(response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load audit logs');
      setLogs([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
  }, []);

  const handleApplyFilter = () => {
    fetchLogs();
  };

  const getOutcomeColor = (outcome: string) => {
    switch (outcome.toLowerCase()) {
      case 'success':
        return 'bg-green-100 text-green-800';
      case 'failure':
        return 'bg-red-100 text-red-800';
      case 'unauthorized':
        return 'bg-yellow-100 text-yellow-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <div>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">Audit Logs</h1>
        <p className="mt-2 text-gray-600">
          Monitor system activity and security events
        </p>
      </div>

      {/* Filters */}
      <div className="card mb-6">
        <div className="flex items-center mb-4">
          <Filter className="h-5 w-5 text-gray-600 mr-2" />
          <h2 className="text-lg font-semibold text-gray-900">Filters</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Filter Type */}
          <div>
            <label className="label">Filter Type</label>
            <select
              className="input"
              value={filterType}
              onChange={(e) => setFilterType(e.target.value as FilterType)}
            >
              <option value="all">All Logs</option>
              <option value="security">Failed Security Events</option>
              <option value="user">By User ID</option>
              <option value="action">By Action</option>
            </select>
          </div>

          {/* Filter Value (conditional) */}
          {(filterType === 'user' || filterType === 'action') && (
            <div>
              <label className="label">
                {filterType === 'user' ? 'User ID' : 'Action Name'}
              </label>
              <input
                type="text"
                className="input"
                placeholder={
                  filterType === 'user'
                    ? 'Enter user ID...'
                    : 'e.g., Login, CreateAgent'
                }
                value={filterValue}
                onChange={(e) => setFilterValue(e.target.value)}
              />
            </div>
          )}

          {/* Start Date */}
          <div>
            <label className="label">Start Date</label>
            <input
              type="date"
              className="input"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
            />
          </div>

          {/* Limit */}
          <div>
            <label className="label">Limit</label>
            <select
              className="input"
              value={limit}
              onChange={(e) => setLimit(Number(e.target.value))}
            >
              <option value={50}>50</option>
              <option value={100}>100</option>
              <option value={250}>250</option>
              <option value={500}>500</option>
              <option value={1000}>1000</option>
            </select>
          </div>
        </div>

        <div className="mt-4">
          <button onClick={handleApplyFilter} className="btn-primary">
            Apply Filters
          </button>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start">
          <AlertCircle className="h-5 w-5 text-red-600 mt-0.5 mr-2 flex-shrink-0" />
          <span className="text-sm text-red-800">{error}</span>
        </div>
      )}

      {/* Loading State */}
      {loading && (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
        </div>
      )}

      {/* Logs Table */}
      {!loading && (
        <div className="card overflow-hidden">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">
              Logs ({logs.length})
            </h2>
          </div>

          {logs.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
              <p>No audit logs found matching your criteria.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="table">
                <thead>
                  <tr>
                    <th>
                      <Calendar className="h-4 w-4 inline mr-1" />
                      Timestamp
                    </th>
                    <th>
                      <User className="h-4 w-4 inline mr-1" />
                      User
                    </th>
                    <th>
                      <Activity className="h-4 w-4 inline mr-1" />
                      Action
                    </th>
                    <th>Entity</th>
                    <th>Outcome</th>
                    <th>Details</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {logs.map((log) => (
                    <tr key={log.id} className="hover:bg-gray-50">
                      <td className="text-xs">
                        {format(new Date(log.timestamp), 'MMM dd, yyyy HH:mm:ss')}
                      </td>
                      <td>
                        <div>
                          <div className="font-medium">{log.username}</div>
                          {log.ipAddress && (
                            <div className="text-xs text-gray-500">
                              {log.ipAddress}
                            </div>
                          )}
                        </div>
                      </td>
                      <td>
                        <span className="font-medium text-gray-900">
                          {log.action}
                        </span>
                      </td>
                      <td>
                        <div>
                          <div className="font-medium text-gray-900">
                            {log.entityType}
                          </div>
                          {log.entityId && (
                            <div className="text-xs text-gray-500 font-mono">
                              {log.entityId.substring(0, 8)}...
                            </div>
                          )}
                        </div>
                      </td>
                      <td>
                        <span
                          className={`px-2 py-1 text-xs font-medium rounded ${getOutcomeColor(
                            log.outcome
                          )}`}
                        >
                          {log.outcome}
                        </span>
                      </td>
                      <td className="text-sm text-gray-600 max-w-md truncate">
                        {log.details || '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default AuditLogs;
