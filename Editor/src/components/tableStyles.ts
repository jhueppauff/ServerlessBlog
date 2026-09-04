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
