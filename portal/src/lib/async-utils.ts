export function pause(time: number = 0) {
  return new Promise((resolve) => {
    setTimeout(resolve, time);
  });
}
