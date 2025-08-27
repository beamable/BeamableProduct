import { ContentBase } from '@/contents/types/ContentBase';

export type ApiContent = ContentBase<{
  description?: {
    data: string;
  };
  method: {
    data: string;
  };
  route: {
    data: {
      service: string;
      endpoint: string;
      serviceTypeStr: string;
    };
  };
  variables: {
    data: {
      variables: {
        name: string;
        typeName: string;
      }[];
    };
  };
  parameters: {
    data: {
      parameters: {
        name: string;
        variableRef?: {
          name: string;
        };
        body: string;
        typeName: string;
      }[];
    };
  };
}>;
