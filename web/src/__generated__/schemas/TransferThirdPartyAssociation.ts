import { ThirdPartyAssociation } from './ThirdPartyAssociation';

export type TransferThirdPartyAssociation = { 
  fromAccountId: bigint | string; 
  thirdParty: ThirdPartyAssociation; 
};
