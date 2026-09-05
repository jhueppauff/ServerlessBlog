import { Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField } from '@mui/material';

export const PublishDialog = ({
  open,
  publishDate,
  onDateChange,
  onCancel,
  onPublish,
}: {
  open: boolean;
  publishDate: string;
  onDateChange: (value: string) => void;
  onCancel: () => void;
  onPublish: () => void;
}) => (
  <Dialog open={open} onClose={onCancel}>
    <DialogTitle>Publish Dialog</DialogTitle>
    <DialogContent>
      <TextField
        type="date"
        label="Select Publish Date"
        value={publishDate}
        onChange={(event) => onDateChange(event.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
        sx={{ mt: 1 }}
      />
    </DialogContent>
    <DialogActions>
      <Button variant="contained" color="success" onClick={onPublish}>
        Publish
      </Button>
      <Button variant="contained" color="warning" onClick={onCancel}>
        Close
      </Button>
    </DialogActions>
  </Dialog>
);
