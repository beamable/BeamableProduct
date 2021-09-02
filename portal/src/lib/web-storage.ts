const {
  localStorage
} = window;

export function load(key: string, value?: any): any {
  try {
    const json = localStorage.getItem(key);

    if (json !== null) {
      value = JSON.parse(json);
    }
  } catch(err) {
    save(key, null);
  }

  return value;
}

export function save(key: string, value: any): boolean {
  try {
    if (value === null || typeof value === 'undefined') {
      localStorage.removeItem(key);
    } else {
      const json = JSON.stringify(value);
      localStorage.setItem(key, json);
    }

    return true;
  } catch(err) {
    return false;
  }
}

export default { load, save };
