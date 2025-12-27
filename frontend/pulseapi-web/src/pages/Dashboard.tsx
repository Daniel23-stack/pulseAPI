import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import apiClient from '../api/client';

interface Metrics {
  totalTrafficTps: number;
  errorRatePercent: number;
  topProxyLatencyP99Ms: number;
  alertCount: number;
  apiBreakdown: ApiMetrics[];
  alertBreakdown: AlertBreakdown[];
}

interface ApiMetrics {
  apiId: number;
  apiName: string;
  environment: string;
  trafficTps: number;
  errorRatePercent: number;
  latencyP99Ms: number;
}

interface AlertBreakdown {
  alertName: string;
  count: number;
}

const Dashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<Metrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [environment, setEnvironment] = useState('prod');

  useEffect(() => {
    loadMetrics();
    const interval = setInterval(loadMetrics, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, [environment]);

  const loadMetrics = async () => {
    try {
      const response = await apiClient.get('/metrics', {
        params: { environment },
      });
      setMetrics(response.data);
    } catch (error) {
      console.error('Failed to load metrics:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading || !metrics) {
    return <div className="p-8">Loading...</div>;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white shadow">
        <div className="px-8 py-6">
          <h1 className="text-3xl font-bold text-gray-900">API Monitoring</h1>
          <p className="mt-2 text-sm text-gray-600">
            Last hour for apigee-pinpoint:{' '}
            <select
              value={environment}
              onChange={(e) => setEnvironment(e.target.value)}
              className="text-indigo-600 font-medium cursor-pointer"
            >
              <option value="prod">prod</option>
              <option value="staging">staging</option>
              <option value="dev">dev</option>
            </select>
          </p>
        </div>
      </div>

      <div className="px-8 py-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <MetricCard
            title="Total Traffic"
            value={`${metrics.totalTrafficTps.toFixed(3)} TPS`}
            icon="📊"
            breakdown={metrics.apiBreakdown.map(a => `${a.apiName} (${a.environment}): ${a.trafficTps.toFixed(3)}`)}
          />
          <MetricCard
            title="Error Rate"
            value={`${metrics.errorRatePercent.toFixed(2)}%`}
            icon="⚠️"
            breakdown={metrics.apiBreakdown.map(a => `${a.apiName} (${a.environment}): ${a.errorRatePercent.toFixed(3)}`)}
          />
          <MetricCard
            title="Top Proxy Latency P99"
            value={`${metrics.topProxyLatencyP99Ms} MS`}
            icon="⏱️"
            breakdown={metrics.apiBreakdown.map(a => `${a.apiName} (${a.environment}): ${a.latencyP99Ms}`)}
          />
          <MetricCard
            title="Alerts"
            value={metrics.alertCount.toString()}
            icon="🔔"
            breakdown={metrics.alertBreakdown.map(a => `${a.alertName}: ${a.count}`)}
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-6">
          <NavCard title="Recent" description="Track anomalies for the last hour" icon="🕐" link="/recent" />
          <NavCard title="Timeline" description="View trends history for context" icon="📅" link="/timeline" />
          <NavCard title="Investigate" description="Drilldown and diagnose from logs" icon="🔍" link="/investigate" />
          <NavCard title="Alerts" description="Configure alerts and get notified of issues" icon="🔔" link="/alerts" />
          <NavCard title="Collections" description="Create Collection to monitor group of proxies and targets" icon="📦" link="/collections" />
        </div>
      </div>
    </div>
  );
};

const MetricCard: React.FC<{ title: string; value: string; icon: string; breakdown: string[] }> = ({
  title,
  value,
  icon,
  breakdown,
}) => {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-medium text-gray-900">{title}</h3>
        <span className="text-2xl">{icon}</span>
      </div>
      <div className="text-3xl font-bold text-gray-900 mb-4">{value}</div>
      <div className="space-y-1">
        {breakdown.slice(0, 2).map((item, idx) => (
          <div key={idx} className="text-sm text-gray-600">{item}</div>
        ))}
      </div>
      <a href="#" className="text-sm text-indigo-600 hover:text-indigo-800 mt-2 block">View all</a>
    </div>
  );
};

const NavCard: React.FC<{ title: string; description: string; icon: string; link: string }> = ({
  title,
  description,
  icon,
  link,
}) => {
  return (
    <Link
      to={link}
      className="bg-white rounded-lg shadow p-6 hover:shadow-lg transition-shadow cursor-pointer"
    >
      <div className="text-4xl mb-4">{icon}</div>
      <h3 className="text-lg font-medium text-gray-900 mb-2">{title}</h3>
      <p className="text-sm text-gray-600">{description}</p>
    </Link>
  );
};

export default Dashboard;



