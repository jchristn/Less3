export const apiEndpointURL = process.env.NEXT_PUBLIC_LESS3_SERVER_URL || 'http://127.0.0.1:8000';

export const MIN_PASSWORD_LENGTH = 8;

export const keepUnusedDataFor = 300; //5mins

// Bundled observability stack endpoints. Configurable via env; sensible local defaults otherwise.
export const grafanaUrl = process.env.NEXT_PUBLIC_LESS3_GRAFANA_URL || 'http://localhost:3001';
export const prometheusUrl = process.env.NEXT_PUBLIC_LESS3_PROMETHEUS_URL || 'http://localhost:9090';

// Bundled Clutch lock-management dashboard endpoints. Configurable via env; sensible local defaults otherwise.
export const clutchUiUrl = process.env.NEXT_PUBLIC_LESS3_CLUTCH_UI_URL || 'http://localhost:3002';
export const clutchApiUrl = process.env.NEXT_PUBLIC_LESS3_CLUTCH_API_URL || 'http://localhost:8080';

// Polling interval (ms) for live cluster/lock operational views.
export const clusterPollingIntervalMs = 5000;
