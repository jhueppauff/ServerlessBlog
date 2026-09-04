import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';
import DeleteForeverIcon from '@mui/icons-material/DeleteForever';
import type { PostMetadata } from '../api/models';

export const DeletePostDialog = ({
  post,
  onCancel,
  onConfirm,
}: {
  post?: PostMetadata;
  onCancel: () => void;
  onConfirm: (post: PostMetadata) => void;
}) => (
  <Dialog open={post !== undefined} onClose={onCancel}>
    <DialogTitle>
      <DeleteForeverIcon sx={{ mr: 1, mb: -0.5 }} />
      Delete Post?
    </DialogTitle>
    <DialogContent>
      <Stack spacing={2} sx={{ mt: 1 }}>
        <TextField label="Title" value={post?.title ?? ''} slotProps={{ input: { readOnly: true } }} />
        <TextField
          label="Published"
          value={post?.published ?? ''}
          slotProps={{ input: { readOnly: true } }}
        />
      </Stack>
    </DialogContent>
    <DialogActions>
      <Button onClick={onCancel}>Cancel</Button>
      <Button color="error" onClick={() => post && onConfirm(post)}>
        Delete Post
      </Button>
    </DialogActions>
  </Dialog>
);
