import UserNotifications
import Foundation

/// Downloads a remote media asset referenced by the push payload and attaches it to the
/// notification (feature 7, rich media — the `bigPicture` style). Looks for the URL under the
/// canonical `imageUrl` key first (the shared wire contract emitted by `PushRailService`), then
/// falls back to the legacy `media-url` / nested `bmn.mediaUrl` aliases. Silently passes through
/// if there is nothing to attach.
public final class RichMediaServicePlugin: NotificationServicePlugin {

    public init() {}

    public func process(_ content: UNMutableNotificationContent,
                        completion: @escaping (UNMutableNotificationContent) -> Void) {
        guard let urlString = (content.userInfo["imageUrl"] as? String)
                ?? (content.userInfo["media-url"] as? String)
                ?? ((content.userInfo["bmn"] as? [String: Any])?["mediaUrl"] as? String),
              let url = URL(string: urlString) else {
            completion(content)
            return
        }

        let task = URLSession.shared.downloadTask(with: url) { tempURL, response, _ in
            defer { /* fall through to completion in all branches below */ }
            guard let tempURL = tempURL else { completion(content); return }

            // Give the temp file a sensible extension so the OS can render it.
            let ext = url.pathExtension.isEmpty ? "tmp" : url.pathExtension
            let dest = tempURL.deletingPathExtension().appendingPathExtension(ext)
            try? FileManager.default.moveItem(at: tempURL, to: dest)

            if let attachment = try? UNNotificationAttachment(identifier: "media", url: dest, options: nil) {
                content.attachments = [attachment]
            }
            completion(content)
        }
        task.resume()
    }
}
