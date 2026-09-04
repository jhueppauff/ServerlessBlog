import { useEffect, useMemo, useState } from 'react';
import {
  Box,
  Checkbox,
  Chip,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  OutlinedInput,
  Paper,
  Select,
  type SelectChangeEvent,
} from '@mui/material';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { useBlogClient } from '../api/BlogClientProvider';
import type { PageMetric, PostMetadata } from '../api/models';
import { useNotification } from '../components/NotificationProvider';
import { buildChartRows, lastDays, seriesColor } from './metricsData';

export const Metrics = () => {
  const client = useBlogClient();
  const notify = useNotification();
  const [posts, setPosts] = useState<PostMetadata[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [history, setHistory] = useState<Record<string, PageMetric[]>>({});

  const days = useMemo(() => lastDays(31), []);

  useEffect(() => {
    client
      .getBlogPosts()
      .then(setPosts)
      .catch((error: Error) => notify(`An error occured: ${error.message}`, 'error'));
  }, [client, notify]);

  const onSelectionChanged = async (event: SelectChangeEvent<string[]>) => {
    const value = event.target.value;
    const slugs = typeof value === 'string' ? value.split(',') : value;
    setSelected(slugs);

    const loaded: Record<string, PageMetric[]> = {};

    try {
      await Promise.all(
        slugs.map(async (slug) => {
          loaded[slug] = history[slug] ?? (await client.getPageViewHistory(slug));
        }),
      );

      setHistory(loaded);
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    }
  };

  const rows = useMemo(() => buildChartRows(days, history), [days, history]);

  return (
    <>
      <FormControl fullWidth>
        <InputLabel id="posts-label">Posts</InputLabel>
        <Select
          labelId="posts-label"
          multiple
          value={selected}
          onChange={(event) => void onSelectionChanged(event)}
          input={<OutlinedInput label="Posts" />}
          renderValue={(values) => (
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {values.map((value) => (
                <Chip key={value} label={value} size="small" />
              ))}
            </Box>
          )}
        >
          {posts.map((post) => (
            <MenuItem key={post.slug} value={post.slug}>
              <Checkbox checked={selected.includes(post.slug)} />
              <ListItemText primary={post.slug} />
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {selected.length > 0 && (
        <Paper sx={{ mt: 2, p: 2, height: 400 }}>
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={rows}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="label" />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Legend />
              {selected.map((slug, index) => (
                <Line key={slug} type="monotone" dataKey={slug} stroke={seriesColor(index)} />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </Paper>
      )}
    </>
  );
};
