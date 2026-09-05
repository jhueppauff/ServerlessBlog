export interface PostMetadata {
  partitionKey: string;
  rowKey: string;
  slug: string;
  title: string;
  published: string;
  tags: string;
  preview: string;
  imageUrl: string;
  pageUrl?: string;
  views: number;
  isPublic: boolean;
}

export interface Blob {
  name: string;
  url: string;
}

export interface PageMetric {
  slug: string;
  views: number;
  timestamp: string;
}

export interface PublishRequest {
  slug: string;
  publishDate: string;
}

export const emptyPost = (): PostMetadata => ({
  partitionKey: '',
  rowKey: '',
  slug: '',
  title: '',
  published: '',
  tags: '',
  preview: '',
  imageUrl: '',
  pageUrl: '',
  views: 0,
  isPublic: false,
});
