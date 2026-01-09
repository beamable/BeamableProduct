## How to add a new Service

1. **Create your service file**  
   Inside `src/services/`, create a new file called `<ServiceName>Service.ts`.

2. **Subclass `ApiService`**  
   Copy the template below into the new file:

   ```ts
   import {
     ApiService,
     type ApiServiceProps,
   } from '@/services/types/ApiService';

   export class ExampleService extends ApiService {
     constructor(props: ApiServiceProps) {
       super(props);
     }

     /** @internal */
     get serviceName(): string {
       return 'example'; // <— this must match the generated API group name
     }

     // add service methods here. For example:
     // async getFoo(): Promise<FooView> {
     //   const { body } = await fooGetById(this.requester, { id: 'foo' }, this.accountId);
     //   return body;
     // }
   }
   ```
