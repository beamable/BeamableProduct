import { render } from "@testing-library/svelte";
import common from '../../fixtures/common-setup';
const {config} = common();

import ComponentButtons from './ComponentButtons.svelte';


describe("Component Button Component", () => {
    const original = console.error

    beforeEach(() => {
        console.error = jest.fn();
    });

    afterEach(() => {
        console.error = original;
    });

    test("should log error not in dev when not under relative component", () => {
        config({
            dev: false
        });
        render(ComponentButtons);
        expect(console.error).toBeCalled();
    });

    test("should throw error in dev when not under relative component", () => {
        config({
            dev: true
        });
        const renderFunc = () => render(ComponentButtons);
        expect(renderFunc).toThrowError();
    });

    test("should not throw error in dev when under relative component", () => {
        config({
            dev: true
        });
        const container = document.createElement('div');
        container.style.position = 'relative';
        const renderFunc = () => render(ComponentButtons, {}, {
            container,
        });
        expect(renderFunc).not.toThrowError();
    });
});
