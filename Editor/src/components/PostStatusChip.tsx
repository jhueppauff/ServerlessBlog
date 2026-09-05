import { Box, Tooltip, Typography } from '@mui/material';
import { formatDate } from './tableStyles';

type PostStatusChipProps = {
  isPublic?: boolean;
  /** The date the post was or is going to be published on. */
  published?: string;
};

/**
 * Compact publication indicator: a coloured dot on phones, dot plus label from
 * the `sm` breakpoint upwards, so the status column stays narrow.
 */
export const PostStatusChip = ({ isPublic, published }: PostStatusChipProps) => {
  const label = isPublic ? 'Published' : 'Draft';
  const color = isPublic ? 'success.main' : 'text.disabled';

  return (
    <Tooltip title={isPublic ? `Published on ${formatDate(published)}` : 'Not visible on the blog yet'}>
      <Box
        component="span"
        aria-label={label}
        sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75 }}
      >
        <Box
          component="span"
          sx={{
            width: 10,
            height: 10,
            borderRadius: '50%',
            flexShrink: 0,
            bgcolor: isPublic ? color : 'transparent',
            border: 2,
            borderColor: color,
          }}
        />
        <Typography
          variant="caption"
          sx={{ display: { xs: 'none', sm: 'inline' }, color, whiteSpace: 'nowrap' }}
        >
          {label}
        </Typography>
      </Box>
    </Tooltip>
  );
};
