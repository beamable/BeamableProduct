import { AnnouncementContent } from '@/contents/types/AnnouncementContent';
import { ApiContent } from '@/contents/types/ApiContent';
import { CalendarContent } from '@/contents/types/CalendarContent';
import { CurrencyContent } from '@/contents/types/CurrencyContent';
import { EmailContent } from '@/contents/types/EmailContent';
import { EventContent } from '@/contents/types/EventContent';
import { GameTypeContent } from '@/contents/types/GameTypeContent';
import { DonationContent } from '@/contents/types/DonationContent';
import { ItemContent } from '@/contents/types/ItemContent';
import { LeaderboardContent } from '@/contents/types/LeaderboardContent';
import { ListingContent } from '@/contents/types/ListingContent';
import { SKUContent } from '@/contents/types/SKUContent';
import { StoreContent } from '@/contents/types/StoreContent';
import { TournamentContent } from '@/contents/types/TournamentContent';
import { VipContent } from '@/contents/types/VipContent';

/** Define a mapping of content prefixes to their corresponding types */
export interface ContentTypeMap {
  announcements: AnnouncementContent;
  api: ApiContent;
  calendars: CalendarContent;
  currency: CurrencyContent;
  donations: DonationContent;
  emails: EmailContent;
  events: EventContent;
  game_types: GameTypeContent;
  items: ItemContent;
  leaderboards: LeaderboardContent;
  listings: ListingContent;
  skus: SKUContent;
  stores: StoreContent;
  tournaments: TournamentContent;
  vip: VipContent;
}
