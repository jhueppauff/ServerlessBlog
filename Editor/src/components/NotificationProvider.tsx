import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { Alert, Snackbar, type AlertColor } from '@mui/material';

type Notify = (message: string, severity?: AlertColor) => void;

const NotificationContext = createContext<Notify | undefined>(undefined);

export const useNotification = (): Notify => {
  const notify = useContext(NotificationContext);

  if (!notify) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }

  return notify;
};

export const NotificationProvider = ({ children }: { children: ReactNode }) => {
  const [message, setMessage] = useState<string>();
  const [severity, setSeverity] = useState<AlertColor>('info');

  const notify = useCallback<Notify>((text, level = 'info') => {
    setMessage(text);
    setSeverity(level);
  }, []);

  const value = useMemo(() => notify, [notify]);

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <Snackbar
        open={message !== undefined}
        autoHideDuration={10000}
        onClose={() => setMessage(undefined)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Alert severity={severity} variant="filled" onClose={() => setMessage(undefined)}>
          {message}
        </Alert>
      </Snackbar>
    </NotificationContext.Provider>
  );
};
