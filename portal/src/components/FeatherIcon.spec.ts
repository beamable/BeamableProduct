import { render } from "@testing-library/svelte";

import common from '../../fixtures/common-setup';
common();

import feather from 'feather-icons';
import FeatherIcon from './FeatherIcon.svelte';

describe("FeatherIcon Component", () => {
  test("should not break on the import of the feather icon library", () => {
    expect(feather).not.toBeUndefined();
    render(FeatherIcon);
  });
});