import { Chip, Tooltip } from '@mui/material';
import PublicIcon from '@mui/icons-material/Public';
import PublicOffIcon from '@mui/icons-material/PublicOff';
import { formatDate } from './tableStyles';

type PostStatusChipProps = {
  isPublic?: boolean;
  /** The date the post was or is going to be published on. */
  published?: string;
};

export const PostStatusChip = ({ isPublic, published }: PostStatusChipProps) => {
  const label = isPublic ? 'Published' : 'Draft';
  const tooltip = isPublic
    ? `Published on ${formatDate(published)}`
    : 'Not visible on the blog yet';

  return (
    <Tooltip title={tooltip}>
      <Chip
        size="small"
        variant={isPublic ? 'filled' : 'outlined'}
        color={isPublic ? 'success' : 'default'}
        icon={isPublic ? <PublicIcon /> : <PublicOffIcon />}
        label={label}
        sx={{ fontWeight: 500 }}
      />
    </Tooltip>
  );
};
