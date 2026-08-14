import { screen } from '@testing-library/react';
import ObservabilityPage from '#/page/observability/ObservabilityPage';
import { renderWithRedux } from '../store/utils';

describe('ObservabilityPage', () => {
  it('links to Grafana and Prometheus and names the three bundled dashboards', () => {
    renderWithRedux(<ObservabilityPage />, false, undefined, true);

    expect(screen.getByText('Grafana')).toBeInTheDocument();
    expect(screen.getByText('Prometheus')).toBeInTheDocument();

    const grafanaLink = screen.getByRole('link', { name: /localhost:3001/i });
    expect(grafanaLink).toHaveAttribute('href', 'http://localhost:3001');
    expect(grafanaLink).toHaveAttribute('target', '_blank');

    const prometheusLink = screen.getByRole('link', { name: /localhost:9090/i });
    expect(prometheusLink).toHaveAttribute('href', 'http://localhost:9090');

    // The three bundled Grafana dashboards
    expect(screen.getByText('Overview')).toBeInTheDocument();
    expect(screen.getByText('Locks & Data Integrity')).toBeInTheDocument();
    expect(screen.getByText('Cluster')).toBeInTheDocument();
  });
});
