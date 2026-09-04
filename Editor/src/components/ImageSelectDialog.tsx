import { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
} from '@mui/material';
import { useBlogClient } from '../api/BlogClientProvider';
import type { Blob } from '../api/models';
import { Loading } from './Loading';

export const ImageSelectDialog = ({
  open,
  onClose,
  onSelect,
}: {
  open: boolean;
  onClose: () => void;
  onSelect: (url: string) => void;
}) => {
  const client = useBlogClient();
  const [images, setImages] = useState<Blob[]>();

  useEffect(() => {
    if (!open) {
      return;
    }

    setImages(undefined);
    void client.getBlobs().then(setImages);
  }, [client, open]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Image search dialog</DialogTitle>
      <DialogContent>
        {images ? (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, justifyContent: 'center' }}>
            {images.map((image) => (
              <Paper key={image.name} sx={{ height: 140, width: 140, overflow: 'hidden' }}>
                <Box
                  component="img"
                  src={image.url}
                  alt={image.name}
                  sx={{ width: '100%', cursor: 'pointer' }}
                  onClick={() => onSelect(image.url)}
                />
              </Paper>
            ))}
          </Box>
        ) : (
          <Loading />
        )}
      </DialogContent>
      <DialogActions>
        <Button variant="contained" color="warning" onClick={onClose}>
          Close
        </Button>
      </DialogActions>
    </Dialog>
  );
};
