export type GroupUserMember = { 
  id: bigint | string; 
  subGroups: GroupUserMember[]; 
  joined?: bigint | string; 
};
