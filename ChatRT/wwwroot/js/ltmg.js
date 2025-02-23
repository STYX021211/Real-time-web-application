export class MGame {

    #rows
    #columns
    #numOfMines
    #gridMeta
    #gameWon
    #gameOver

    constructor(height, width, numOfMines, gridId) {
        this.#rows = height;
        this.#columns = width;
        this.#numOfMines = numOfMines;
        this.#gridMeta = [];
        this.#gameWon = false;   
        this.#gameOver = false;

        this.buildBoard(gridId);
        
    }

    buildBoard(gridId) {
        const table = document.querySelector(gridId);
        const cellsToBeMined = [];

        for (let rowIndex = 0; rowIndex < this.#rows; rowIndex++) {
            const row = document.createElement('tr');
            table.appendChild(row);
            this.#gridMeta.push([]);
            for (let colIndex = 0; colIndex < this.#columns; colIndex++) {
                const cell = document.createElement('td');
                cell.innerText = '⬛';
                cell.dataset.number = 0;
                cell.dataset.bomb = false;
                cell.dataset.revealed = false;

                cell.addEventListener('contextMenu', event => event.preventDefault(), false);
                
                cell.addEventListener('mousedown', event => {

                    if (event.button == 0 ){
                        this.reveal(cell);
                    }else if (event.button ==2 ) {
                        this.flag(cell);
                    }
                });

                cellsToBeMined.push({ rowIndex, colIndex, cell });
                this.#gridMeta[rowIndex].push(cell);

                row.appendChild(cell);
            }
        } 

        // Shuffle Array
        cellsToBeMined.sort(() => 0.5 - Math.random()) // get shuffle on array
                        .slice(0, this.#numOfMines)
                        .forEach(cellData => {
                            cellData.cell.dataset.bomb = true;
                            this.safeIncr(cellData.rowIndex+1, cellData.colIndex);
                            this.safeIncr(cellData.rowIndex+1, cellData.colIndex+1);
                            this.safeIncr(cellData.rowIndex+1, cellData.colIndex-1);

                            this.safeIncr(cellData.rowIndex-1, cellData.colIndex);
                            this.safeIncr(cellData.rowIndex-1, cellData.colIndex+1);
                            this.safeIncr(cellData.rowIndex-1, cellData.colIndex-1);
                            this.safeIncr(cellData.rowIndex, cellData.colIndex+1);
                            this.safeIncr(cellData.rowIndex, cellData.colIndex-1);
                        });

                    // this.revealAll();
                }
                safeIncr( row, col ) {
                    if(row >= this.#rows || col >= this.#columns || row < 0 || col < 0) {
                        return;
                }
                this.#gridMeta[row][col].dataset.number++;
            }

            revealAll(){
                this.#gridMeta.forEach( row => row.forEach (cell => 
                        this.reveal(cell)
                    )
                );
            }

            reveal ( cell ) {
                const nums = ["🟦", "1️⃣","2️⃣","3️⃣","4️⃣","5️⃣","6️⃣","7️⃣","8️⃣"];
                        if (cell.dataset.bomb == "true" && this.#gameWon ) {
                            cell.innerText = "💣";

                        } else if (cell.dataset.bomb == "true" ) {
                            cell.innerText = "💥";
                            this.#gameOver = true;
                        } else {
                            cell.innerText = nums[cell.dataset.number];
                        }
                        
                        cell.dataset.revealed = "true";
    }
    flag (cell) {
        if (cell.dataset.revealed == "true") {
            return;
        }
        cell.innerText = "🚩";
    }
}
            
