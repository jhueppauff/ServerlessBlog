import type { ReactElement } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Button, IconButton, Tooltip, useMediaQuery, useTheme } from '@mui/material';

type ToolbarActionProps = {
  label: string;
  icon: ReactElement;
  /** Renders the action as a router link instead of a button. */
  to?: string;
  onClick?: () => void;
  /** The primary action of a toolbar is filled, the others are outlined. */
  primary?: boolean;
};

/**
 * Toolbar action that degrades to an icon button on phones, where a labelled
 * button would push the toolbar onto a second line.
 */
export const ToolbarAction = ({ label, icon, to, onClick, primary }: ToolbarActionProps) => {
  const theme = useTheme();
  const compact = useMediaQuery(theme.breakpoints.down('sm'));
  const linkProps = to ? { component: RouterLink, to } : {};

  return (
    <Tooltip title={label}>
      {compact ? (
        <IconButton
          {...linkProps}
          onClick={onClick}
          aria-label={label}
          color={primary ? 'primary' : 'default'}
        >
          {icon}
        </IconButton>
      ) : (
        <Button
          {...linkProps}
          onClick={onClick}
          aria-label={label}
          startIcon={icon}
          variant={primary ? 'contained' : 'outlined'}
          color={primary ? 'primary' : 'inherit'}
        >
          {label}
        </Button>
      )}
    </Tooltip>
  );
};
