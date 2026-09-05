import type { Breakpoint } from '@mui/material';

/**
 * Hides a table column on viewports smaller than the given breakpoint so the
 * tables stay readable on phones and tablets instead of overflowing.
 */
export const hideBelow = (breakpoint: Breakpoint) => ({
  display: { xs: 'none', [breakpoint]: 'table-cell' },
});

/** Keeps long free text on a single line with an ellipsis. */
export const truncate = {
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
};

/** Allows long urls to wrap instead of stretching the table. */
export const breakAnywhere = {
  overflowWrap: 'anywhere',
  wordBreak: 'break-all',
};

/**
 * Shared look for the data tables: a subtly tinted, sticky header, roomier
 * cells and no divider under the last row.
 */
export const dataTable = {
  '& thead th': {
    backgroundColor: 'action.hover',
    fontWeight: 600,
    whiteSpace: 'nowrap',
  },
  '& tbody td': {
    py: 1.25,
    borderColor: 'divider',
  },
  '& tbody tr:last-of-type td': {
    borderBottom: 0,
  },
};

/** Formats an ISO or date like string for display, falling back to the raw value. */
export const formatDate = (value?: string): string => {
  if (!value) {
    return '—';
  }

  const date = new Date(value);

  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString();
};

/**
 * Toolbar buttons that shrink to icon only on phones, where the label would
 * otherwise push the actions onto a second line.
 */
export const compactButton = {
  minWidth: 0,
  px: { xs: 1, sm: 2 },
  '& .MuiButton-startIcon': { mr: { xs: 0, sm: 1 }, ml: 0 },
};

/** The label of a `compactButton`, hidden on phones. */
export const compactButtonLabel = {
  display: { xs: 'none', sm: 'inline' },
};
