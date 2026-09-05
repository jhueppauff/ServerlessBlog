import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#776be7' },
    background: { default: 'rgb(50, 51, 61)' },
  },
  typography: {
    fontFamily: 'Roboto, Helvetica, Arial, sans-serif',
    button: {
      textTransform: 'none',
      fontWeight: 600,
      letterSpacing: 0,
    },
  },
  shape: { borderRadius: 8 },
  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: 8 },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
      },
    },
  },
});
