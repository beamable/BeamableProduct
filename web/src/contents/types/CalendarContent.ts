import { ContentBase } from '@/contents/types/ContentBase';

export type CalendarContent = ContentBase<{
  start_date?: {
    data: string;
  };
}>;
