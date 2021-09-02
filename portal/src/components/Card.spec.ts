import { render } from "@testing-library/svelte";
import common from '../../fixtures/common-setup';
const {mockServices} = common();

import Card from './Card.svelte';
import { route } from "svelte-filerouter";

describe("Card Component", () => {
  test("should render component correctly", () => {
    mockServices({
      http: {
        isResponseUnavailable: () => false
      }
    });


    const { getByText, getByTestId } = render(Card, {
        title: 'Howdy'
    });

    const x = getByText('Howdy');
    expect(x.textContent).toBe('Howdy');
  });
});
