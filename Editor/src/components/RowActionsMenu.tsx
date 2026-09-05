import { useState, type MouseEvent, type ReactElement } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { IconButton, ListItemIcon, ListItemText, Menu, MenuItem, Tooltip } from '@mui/material';
import MoreVertIcon from '@mui/icons-material/MoreVert';

export type RowAction = {
  label: string;
  icon: ReactElement;
  /** Renders the action as a router link instead of a button. */
  to?: string;
  onClick?: () => void;
  /** Highlights destructive actions. */
  destructive?: boolean;
};

type RowActionsMenuProps = {
  /** Used to give every row menu a unique accessible name. */
  title: string;
  actions: RowAction[];
};

export const RowActionsMenu = ({ title, actions }: RowActionsMenuProps) => {
  const [anchor, setAnchor] = useState<HTMLElement>();

  const close = () => setAnchor(undefined);

  const open = (event: MouseEvent<HTMLElement>) => setAnchor(event.currentTarget);

  return (
    <>
      <Tooltip title={`Actions for ${title}`}>
        <IconButton
          size="small"
          aria-label={`Actions for ${title}`}
          aria-haspopup="menu"
          aria-expanded={anchor !== undefined}
          onClick={open}
        >
          <MoreVertIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      <Menu
        anchorEl={anchor}
        open={anchor !== undefined}
        onClose={close}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        {actions.map((action) => (
          <MenuItem
            key={action.label}
            {...(action.to ? { component: RouterLink, to: action.to } : {})}
            onClick={() => {
              close();
              action.onClick?.();
            }}
            sx={action.destructive ? { color: 'error.main' } : undefined}
          >
            <ListItemIcon sx={action.destructive ? { color: 'error.main' } : undefined}>
              {action.icon}
            </ListItemIcon>
            <ListItemText>{action.label}</ListItemText>
          </MenuItem>
        ))}
      </Menu>
    </>
  );
};
