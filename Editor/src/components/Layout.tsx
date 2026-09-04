import { useEffect, useState } from 'react';
import { Link as RouterLink, Outlet, useLocation } from 'react-router-dom';
import {
  AppBar,
  Box,
  Button,
  Container,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import CameraIcon from '@mui/icons-material/Camera';
import HomeIcon from '@mui/icons-material/Home';
import MenuIcon from '@mui/icons-material/Menu';
import StorageIcon from '@mui/icons-material/Storage';
import { useMsal } from '@azure/msal-react';

const drawerWidth = 240;

const navItems = [
  { href: '/', label: 'Home', icon: <HomeIcon /> },
  { href: '/images', label: 'Images', icon: <StorageIcon /> },
  { href: '/metrics', label: 'Metrics', icon: <CameraIcon /> },
];

export const Layout = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [open, setOpen] = useState(!isMobile);
  const location = useLocation();
  const { instance, accounts } = useMsal();
  const account = accounts[0];

  // Close the drawer when navigating on small screens, where it covers the content.
  useEffect(() => {
    if (isMobile) {
      setOpen(false);
    }
  }, [isMobile, location.pathname]);

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
        <Toolbar>
          <IconButton color="inherit" edge="start" onClick={() => setOpen(!open)} aria-label="menu">
            <MenuIcon />
          </IconButton>
          <Box sx={{ flexGrow: 1 }} />
          {account && (
            <>
              <Typography noWrap sx={{ mr: 2, display: { xs: 'none', sm: 'block' } }}>
                Hello, {account.name ?? account.username}!
              </Typography>
              <Button color="inherit" onClick={() => void instance.logoutRedirect()}>
                Log out
              </Button>
            </>
          )}
        </Toolbar>
      </AppBar>
      <Drawer
        variant={isMobile ? 'temporary' : 'persistent'}
        open={open}
        onClose={() => setOpen(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          width: open && !isMobile ? drawerWidth : 0,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: { width: drawerWidth, boxSizing: 'border-box' },
        }}
      >
        <Toolbar />
        <Typography variant="h5" color="primary" sx={{ mt: 2, ml: 2 }}>
          Blog Editor
        </Typography>
        <List>
          {navItems.map((item) => (
            <ListItemButton
              key={item.href}
              component={RouterLink}
              to={item.href}
              selected={location.pathname === item.href}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
        </List>
      </Drawer>
      <Box component="main" sx={{ flexGrow: 1, minWidth: 0, p: { xs: 1, sm: 2, md: 3 } }}>
        <Toolbar />
        <Container maxWidth={false} disableGutters sx={{ mt: 2 }}>
          <Outlet />
        </Container>
      </Box>
    </Box>
  );
};
