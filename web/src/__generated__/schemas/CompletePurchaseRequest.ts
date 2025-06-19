export type CompletePurchaseRequest = { 
  isoCurrencySymbol: string; 
  priceInLocalCurrency: string; 
  receipt: string; 
  txid: bigint | string; 
};
