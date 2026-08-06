import Foundation

/// Holds registered notification templates (feature 4). A template provides default
/// title/body/subtitle format strings (with `{placeholder}` substitution), sound,
/// category and attachments, so engine code can schedule by `templateId` + values
/// instead of repeating full content every time.
public final class TemplateStore {

    public static let shared = TemplateStore()

    private var templates: [String: TemplateSpec] = [:]
    private let lock = NSLock()

    public func register(_ template: TemplateSpec) {
        lock.lock(); defer { lock.unlock() }
        templates[template.id] = template
    }

    public func template(id: String) -> TemplateSpec? {
        lock.lock(); defer { lock.unlock() }
        return templates[id]
    }

    /// Substitute `{key}` occurrences in `format` using `values`. Unknown keys are
    /// left intact so partially-filled templates remain debuggable.
    public static func apply(_ format: String?, values: [String: String]) -> String? {
        guard let format = format else { return nil }
        var result = format
        for (key, value) in values {
            result = result.replacingOccurrences(of: "{\(key)}", with: value)
        }
        return result
    }

    /// Merge a template into a request, filling any fields the request left unset.
    /// Explicit request fields always win over template defaults.
    public func resolve(_ request: LocalRequest) -> LocalRequest {
        guard let templateId = request.templateId,
              let template = template(id: templateId) else {
            return request
        }
        let values = request.templateValues ?? [:]
        var resolved = request

        if resolved.title == nil {
            resolved.title = TemplateStore.apply(template.titleFormat, values: values)
        }
        if resolved.body == nil {
            resolved.body = TemplateStore.apply(template.bodyFormat, values: values)
        }
        if resolved.subtitle == nil {
            resolved.subtitle = TemplateStore.apply(template.subtitleFormat, values: values)
        }
        if resolved.sound == nil { resolved.sound = template.sound }
        if resolved.categoryId == nil { resolved.categoryId = template.categoryId }
        if resolved.badge == nil { resolved.badge = template.badge }
        if resolved.attachments == nil { resolved.attachments = template.defaultAttachments }
        return resolved
    }
}
