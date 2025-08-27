import { ContentBase } from '@/contents/types/ContentBase';

export type EmailContent = ContentBase<{
  subject: {
    data: string;
  };
  body: {
    data: string;
  };
}>;
