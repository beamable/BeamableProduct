import { MAX_ATTEMPTS, TIPS, WORD_LENGTH } from '@app/game/constants.ts';
import { useGrid } from '@app/components/hooks/useGrid.ts';

export const Grid = () => {
  const { tipTextRef, setTilesRef, setGridRowsRef } = useGrid();

  return (
    <div className="grid-container">
      <div className="grid-group">
        {Array.from({ length: MAX_ATTEMPTS }, (_, row) => {
          return (
            <div
              key={row.toString()}
              className="grid-row"
              data-animation="idle"
              ref={setGridRowsRef(row)}
            >
              {Array.from({ length: WORD_LENGTH }).map((_, col) => (
                <div
                  key={col.toString()}
                  className="tile"
                  data-animation="idle"
                  ref={setTilesRef(row, col)}
                ></div>
              ))}
            </div>
          );
        })}
      </div>
      <p className="tips" ref={tipTextRef}>
        {TIPS}
      </p>
    </div>
  );
};
