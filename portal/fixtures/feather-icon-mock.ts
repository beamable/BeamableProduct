export default function(icons:Array<string>=['test']) {
    jest.mock('feather-icons', () => ({
        default: {
            icons,
        },
    }));
}