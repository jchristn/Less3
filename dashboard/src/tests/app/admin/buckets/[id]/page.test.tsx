import { render, screen } from '@testing-library/react';
import Page from '#/app/admin/buckets/[id]/page';

jest.mock('#/page/buckets/BucketDetailPage', () => {
  return function MockBucketDetailPage() {
    return <div>Bucket Detail Page</div>;
  };
});

describe('Bucket Detail Page Route', () => {
  it('renders BucketDetailPage', () => {
    render(<Page />);

    expect(screen.getByText('Bucket Detail Page')).toBeInTheDocument();
  });
});
