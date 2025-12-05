export type ServerEventType =
  | 'tournaments.cycle_change'
  | 'content.manifest'
  | 'realm-config.refresh'
  | 'beamo.service_registration_changed'
  | 'event.phase'
  | (string & {});
