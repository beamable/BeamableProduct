import UserNotifications
import Foundation

/// Downloads a remote media asset referenced by the push payload and attaches it to the
/// notification (feature 7, rich media — the `bigPicture` style). Looks for the URL under the
/// canonical `imageUrl` key first (the shared wire contract emitted by `PushRailService`), then
/// falls back to the legacy `media-url` / nested `bmn.mediaUrl` aliases. Silently passes through
/// if there is nothing to attach.
public final class RichMediaServicePlugin: NotificationServicePlugin {

    public init() {}

    /// Common image MIME types → (file extension, UTI). Used to give the downloaded file a type the
    /// OS can render when the URL itself has no usable extension — e.g. an avatar / signed CDN link
    /// like `https://avatars.githubusercontent.com/u/123?v=4`. Without this the file falls back to
    /// `.tmp`, which `UNNotificationAttachment` can't classify, so the image silently fails to attach.
    private static let mimeMap: [String: (ext: String, uti: String)] = [
        "image/jpeg": ("jpg", "public.jpeg"),
        "image/jpg": ("jpg", "public.jpeg"),
        "image/png": ("png", "public.png"),
        "image/gif": ("gif", "com.compuserve.gif"),
        "image/webp": ("webp", "org.webmproject.webp"),
        "image/heic": ("heic", "public.heic"),
    ]

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
            guard let tempURL = tempURL else { completion(content); return }

            // Skip non-2xx responses so we never attach an error page as if it were an image.
            if let http = response as? HTTPURLResponse, !(200...299).contains(http.statusCode) {
                completion(content)
                return
            }

            // Determine the media type. Prefer a real path extension on the URL; otherwise derive it
            // from the response's MIME type. Many image URLs (avatars, signed CDN links) carry no
            // extension — without a type hint `UNNotificationAttachment` can't infer the type and the
            // attachment silently fails, so we also pass an explicit UTI hint when the MIME is known.
            let mapped = response?.mimeType?.lowercased().flatMap { Self.mimeMap[$0] }
            let ext = url.pathExtension.isEmpty ? (mapped?.ext ?? "tmp") : url.pathExtension
            let dest = tempURL.deletingPathExtension().appendingPathExtension(ext)
            try? FileManager.default.moveItem(at: tempURL, to: dest)

            let options = mapped.map { [UNNotificationAttachmentOptionsTypeHintKey: $0.uti] }
            if let attachment = try? UNNotificationAttachment(identifier: "media", url: dest, options: options) {
                content.attachments = [attachment]
            }
            completion(content)
        }
        task.resume()
    }
}
