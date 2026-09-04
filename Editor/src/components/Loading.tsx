import { Box, CircularProgress } from '@mui/material';

export const Loading = () => (
  <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }} role="progressbar">
    <CircularProgress />
  </Box>
);
