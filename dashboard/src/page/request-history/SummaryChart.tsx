/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useMemo, useRef, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { ReloadOutlined } from '@ant-design/icons';
import Less3Button from '#/components/base/button/Button';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import Less3Tooltip from '#/components/base/tooltip/Tooltip';
import type { RequestHistorySummaryResult, RequestHistorySummaryBucket } from '#/store/slice/requestHistoryTypes';

// ── Types ────────────────────────────────────────────────────────────

interface SummaryChartProps {
  summary: RequestHistorySummaryResult | null;
  timeRange: string;
  onTimeRangeChange: (range: string) => void;
  loading?: boolean;
  onRefresh?: () => void;
}

interface TimeRangeConfig {
  label: string;
  value: string;
  interval: string;
  stepMs: number;
  bucketCount: number;
}

interface QuickRange extends TimeRangeConfig {
  startUtc: Date;
  endUtc: Date;
}

interface ChartBucket {
  timestampUtc: string;
  successCount: number;
  failureCount: number;
}

interface HoveredBucket extends ChartBucket {
  total: number;
  clientX: number;
  clientY: number;
}

// ── Constants ────────────────────────────────────────────────────────

const TIME_RANGES: TimeRangeConfig[] = [
  { label: 'Last Hour', value: 'hour', interval: 'minute', stepMs: 60_000, bucketCount: 60 },
  { label: 'Last Day', value: 'day', interval: '15minute', stepMs: 900_000, bucketCount: 96 },
  { label: 'Last Week', value: 'week', interval: 'hour', stepMs: 3_600_000, bucketCount: 24 * 7 },
  { label: 'Last Month', value: 'month', interval: '6hour', stepMs: 21_600_000, bucketCount: 4 * 30 },
];

const SUCCESS_COLOR = '#22AF79';
const FAILURE_COLOR = '#d9383a';

// ── Helpers (matching Lattice exactly) ───────────────────────────────

function floorToStep(timestamp: number, stepMs: number): number {
  return Math.floor(timestamp / stepMs) * stepMs;
}

function getQuickRange(rangeValue: string): QuickRange {
  const range: TimeRangeConfig = TIME_RANGES.find((entry) => entry.value === rangeValue) || TIME_RANGES[1];
  const endExclusiveMs: number = floorToStep(Date.now(), range.stepMs) + range.stepMs;
  const startMs: number = endExclusiveMs - range.bucketCount * range.stepMs;

  return {
    ...range,
    startUtc: new Date(startMs),
    endUtc: new Date(endExclusiveMs - 1),
  };
}

function buildBuckets(summary: RequestHistorySummaryResult | null, range: QuickRange): ChartBucket[] {
  const apiBuckets: Map<number, RequestHistorySummaryBucket> = new Map(
    (summary?.Data || []).map((bucket) => [
      floorToStep(new Date(bucket.TimestampUtc).getTime(), range.stepMs),
      bucket,
    ])
  );

  const startMs: number = range.startUtc.getTime();

  return Array.from({ length: range.bucketCount }, (_, index) => {
    const timestamp: number = startMs + index * range.stepMs;
    const apiBucket: RequestHistorySummaryBucket | undefined = apiBuckets.get(timestamp);
    return {
      timestampUtc: new Date(timestamp).toISOString(),
      successCount: apiBucket?.SuccessCount || 0,
      failureCount: apiBucket?.FailureCount || 0,
    };
  });
}

function formatChartLabel(timestamp: string, interval: string): string {
  const date: Date = new Date(timestamp);
  if (interval === 'day') {
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
  if (interval === 'hour' || interval === '6hour') {
    return date.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}

function formatTooltipTimestamp(timestamp: string, interval: string): string {
  const date: Date = new Date(timestamp);
  if (interval === 'day') {
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  }
  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// ── Export getQuickRange for use by parent pages ─────────────────────

export { getQuickRange };
export type { QuickRange };

// ── Chart component ──────────────────────────────────────────────────

const SummaryChart: React.FC<SummaryChartProps> = ({ summary, timeRange, onTimeRangeChange, loading = false, onRefresh }) => {
  const [hoveredBucket, setHoveredBucket] = useState<HoveredBucket | null>(null);
  const [tooltipPosition, setTooltipPosition] = useState<{ left: number; top: number } | null>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);

  // Tooltip positioning (matching Lattice)
  useEffect(() => {
    if (!hoveredBucket || !tooltipRef.current) {
      setTooltipPosition(null);
      return;
    }

    const tooltipRect: DOMRect = tooltipRef.current.getBoundingClientRect();
    const viewportPadding: number = 12;
    const pointerOffset: number = 16;
    let left: number = hoveredBucket.clientX + pointerOffset;
    let top: number = hoveredBucket.clientY - tooltipRect.height - pointerOffset;

    if (left + tooltipRect.width + viewportPadding > window.innerWidth) {
      left = hoveredBucket.clientX - tooltipRect.width - pointerOffset;
    }
    if (top < viewportPadding) {
      top = hoveredBucket.clientY + pointerOffset;
    }

    left = Math.max(viewportPadding, Math.min(left, window.innerWidth - tooltipRect.width - viewportPadding));
    top = Math.max(viewportPadding, Math.min(top, window.innerHeight - tooltipRect.height - viewportPadding));

    setTooltipPosition((current) => {
      if (current && current.left === left && current.top === top) return current;
      return { left, top };
    });
  }, [hoveredBucket]);

  const range: QuickRange = getQuickRange(timeRange);
  const buckets: ChartBucket[] = useMemo(() => buildBuckets(summary, range), [summary, timeRange]);

  const maxCount: number = Math.max(1, ...buckets.map((b) => b.successCount + b.failureCount));
  const hasData: boolean = buckets.some((b) => b.successCount > 0 || b.failureCount > 0);

  // Chart dimensions (matching Lattice)
  const chartHeight: number = 240;
  const chartWidth: number = 900;
  const paddingLeft: number = 48;
  const paddingRight: number = 24;
  const paddingTop: number = 20;
  const paddingBottom: number = 42;
  const innerWidth: number = chartWidth - paddingLeft - paddingRight;
  const innerHeight: number = chartHeight - paddingTop - paddingBottom;
  const barWidth: number = Math.max(8, innerWidth / Math.max(buckets.length, 1) - 4);

  const gridRatios: number[] = hasData ? [0, 0.25, 0.5, 0.75, 1] : [0, 1];

  const totalSuccess: number = summary?.TotalSuccess ?? 0;
  const totalFailure: number = summary?.TotalFailure ?? 0;
  const totalRequests: number = totalSuccess + totalFailure;

  return (
    <Less3Card style={{ marginBottom: 16 }}>
      <Less3Flex justify="space-between" align="center" style={{ marginBottom: 12 }}>
        <Less3Text weight={600} fontSize={14}>
          Request Summary
        </Less3Text>
        <Less3Flex gap={8} align="center">
          <Less3Flex gap={4}>
            {TIME_RANGES.map((config) => (
              <button
                key={config.value}
                onClick={() => onTimeRangeChange(config.value)}
                style={{
                  padding: '4px 12px',
                  fontSize: 12,
                  border: '1px solid',
                  borderColor: timeRange === config.value ? SUCCESS_COLOR : '#d9d9d9',
                  borderRadius: 4,
                  background: timeRange === config.value ? SUCCESS_COLOR : 'transparent',
                  color: timeRange === config.value ? '#fff' : 'inherit',
                  cursor: 'pointer',
                  fontWeight: timeRange === config.value ? 600 : 400,
                }}
              >
                {config.label}
              </button>
            ))}
          </Less3Flex>
          {onRefresh && (
            <Less3Tooltip title="Refresh" placement="top">
              <Less3Button type="text" icon={<ReloadOutlined spin={loading} />} size="small" onClick={onRefresh} />
            </Less3Tooltip>
          )}
        </Less3Flex>
      </Less3Flex>

      {loading && !summary ? (
        <div style={{ textAlign: 'center', padding: '60px 0' }}>
          <Less3Text type="secondary">Loading chart data...</Less3Text>
        </div>
      ) : (
        <svg
          viewBox={`0 0 ${chartWidth} ${chartHeight}`}
          style={{ width: '100%', height: 'auto', display: 'block' }}
        >
          {/* Grid lines and Y-axis labels */}
          {gridRatios.map((ratio) => {
            const y: number = paddingTop + innerHeight - innerHeight * ratio;
            const label: number = hasData ? Math.round(maxCount * ratio) : 0;
            return (
              <g key={ratio}>
                <line x1={paddingLeft} x2={chartWidth - paddingRight} y1={y} y2={y} stroke="#e8e8e8" strokeWidth={0.5} />
                <text x={paddingLeft - 10} y={y + 4} textAnchor="end" fontSize={10} fill="#8c8c8c">
                  {label}
                </text>
              </g>
            );
          })}

          {/* Bars (matching Lattice positioning exactly) */}
          {buckets.map((bucket, index) => {
            const total: number = bucket.successCount + bucket.failureCount;
            const x: number = paddingLeft + index * (innerWidth / Math.max(buckets.length, 1)) + 2;
            const successHeight: number = innerHeight * (bucket.successCount / maxCount);
            const failureHeight: number = innerHeight * (bucket.failureCount / maxCount);
            const totalHeight: number = innerHeight * (total / maxCount);
            const y: number = paddingTop + innerHeight - totalHeight;

            return (
              <g
                key={`${bucket.timestampUtc}-${index}`}
                onMouseEnter={(event) => setHoveredBucket({ ...bucket, total, clientX: event.clientX, clientY: event.clientY })}
                onMouseMove={(event) => setHoveredBucket({ ...bucket, total, clientX: event.clientX, clientY: event.clientY })}
                onMouseLeave={() => setHoveredBucket(null)}
              >
                {/* Invisible hover target */}
                <rect x={x} y={paddingTop} width={barWidth} height={innerHeight} fill="transparent" style={{ cursor: 'pointer' }} />
                {/* Success bar (bottom portion) */}
                <rect x={x} y={paddingTop + innerHeight - successHeight} width={barWidth} height={successHeight} fill={SUCCESS_COLOR} opacity={0.85} rx={3} />
                {/* Failure bar (stacked on top) */}
                <rect x={x} y={y} width={barWidth} height={failureHeight} fill={FAILURE_COLOR} opacity={0.85} rx={3} />
              </g>
            );
          })}

          {/* X-axis labels (matching Lattice: first, last, and every ~5th) */}
          {buckets.map((bucket, index) => {
            const labelStep: number = Math.ceil(buckets.length / 5);
            if (index !== 0 && index !== buckets.length - 1 && index % labelStep !== 0) return null;
            // Skip last label if too close to previous stepped label
            if (index === buckets.length - 1 && index % labelStep !== 0) {
              const prevStep: number = Math.floor(index / labelStep) * labelStep;
              if (index - prevStep < labelStep * 0.5) return null;
            }
            const labelX: number = paddingLeft + index * (innerWidth / Math.max(buckets.length, 1)) + 2 + barWidth / 2;
            return (
              <text
                key={`xlabel-${index}`}
                x={labelX}
                y={chartHeight - 16}
                textAnchor="middle"
                fontSize={9}
                fill="#8c8c8c"
              >
                {formatChartLabel(bucket.timestampUtc, range.interval)}
              </text>
            );
          })}
        </svg>
      )}

      {/* Stat cards */}
      <Less3Flex gap={16} style={{ marginTop: 12 }}>
        <Less3Card size="small" style={{ flex: 1, textAlign: 'center' }}>
          <Less3Text type="secondary" fontSize={12}>Total</Less3Text>
          <div><Less3Text weight={600} fontSize={20}>{totalRequests.toLocaleString()}</Less3Text></div>
        </Less3Card>
        <Less3Card size="small" style={{ flex: 1, textAlign: 'center' }}>
          <Less3Text type="secondary" fontSize={12}>Success</Less3Text>
          <div><Less3Text weight={600} fontSize={20} style={{ color: SUCCESS_COLOR }}>{totalSuccess.toLocaleString()}</Less3Text></div>
        </Less3Card>
        <Less3Card size="small" style={{ flex: 1, textAlign: 'center' }}>
          <Less3Text type="secondary" fontSize={12}>Failed</Less3Text>
          <div><Less3Text weight={600} fontSize={20} style={{ color: FAILURE_COLOR }}>{totalFailure.toLocaleString()}</Less3Text></div>
        </Less3Card>
      </Less3Flex>

      {/* Tooltip via portal (matching Lattice) */}
      {hoveredBucket && typeof document !== 'undefined'
        ? createPortal(
            <div
              ref={tooltipRef}
              style={{
                position: 'fixed',
                ...(tooltipPosition ? { left: tooltipPosition.left, top: tooltipPosition.top } : { left: -9999, top: -9999 }),
                background: 'rgba(0, 0, 0, 0.85)',
                color: '#fff',
                padding: '8px 12px',
                borderRadius: 6,
                fontSize: 12,
                pointerEvents: 'none',
                zIndex: 10000,
                whiteSpace: 'nowrap',
                lineHeight: 1.6,
                display: 'flex',
                flexDirection: 'column',
              }}
            >
              <strong>{formatTooltipTimestamp(hoveredBucket.timestampUtc, range.interval)}</strong>
              <span>Total: {hoveredBucket.total.toLocaleString()}</span>
              <span style={{ color: SUCCESS_COLOR }}>Success: {hoveredBucket.successCount.toLocaleString()}</span>
              <span style={{ color: FAILURE_COLOR }}>Failed: {hoveredBucket.failureCount.toLocaleString()}</span>
            </div>,
            document.body
          )
        : null}
    </Less3Card>
  );
};

export default SummaryChart;
