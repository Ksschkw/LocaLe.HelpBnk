import { useState, useEffect } from 'react'
import Button from '../ui/Button'
import styles from './TicTacToe.module.css'

// MinMax algorithm for unbeatable AI
function checkWinner(squares) {
  const lines = [
    [0, 1, 2], [3, 4, 5], [6, 7, 8], // rows
    [0, 3, 6], [1, 4, 7], [2, 5, 8], // cols
    [0, 4, 8], [2, 4, 6]             // diagonals
  ]
  for (let i = 0; i < lines.length; i++) {
    const [a, b, c] = lines[i]
    if (squares[a] && squares[a] === squares[b] && squares[a] === squares[c]) {
      return squares[a]
    }
  }
  return null
}

function minmax(board, depth, isMaximizing) {
  const winner = checkWinner(board)
  if (winner === 'O') return 10 - depth
  if (winner === 'X') return depth - 10
  if (!board.includes(null)) return 0

  if (isMaximizing) {
    let bestScore = -Infinity
    for (let i = 0; i < 9; i++) {
      if (!board[i]) {
        board[i] = 'O'
        const score = minmax(board, depth + 1, false)
        board[i] = null
        bestScore = Math.max(score, bestScore)
      }
    }
    return bestScore
  } else {
    let bestScore = Infinity
    for (let i = 0; i < 9; i++) {
      if (!board[i]) {
        board[i] = 'X'
        const score = minmax(board, depth + 1, true)
        board[i] = null
        bestScore = Math.min(score, bestScore)
      }
    }
    return bestScore
  }
}

function getBestMove(board) {
  let bestScore = -Infinity
  let move = -1
  for (let i = 0; i < 9; i++) {
    if (!board[i]) {
      board[i] = 'O'
      const score = minmax(board, 0, false)
      board[i] = null
      if (score > bestScore) {
        bestScore = score
        move = i
      }
    }
  }
  return move
}

export default function TicTacToe({ mode = 'bot', boardState, turn, onMove, winnerState, myPlayer }) {
  // Local state for 'bot' mode
  const [board, setBoard] = useState(Array(9).fill(null))
  const [xIsNext, setXIsNext] = useState(true)
  const [status, setStatus] = useState('Your turn (X)')

  // Determine active state based on mode
  const activeBoard = mode === 'bot' ? board : boardState
  const isMyTurn = mode === 'bot' ? xIsNext : turn === myPlayer
  const winner = mode === 'bot' ? checkWinner(board) : winnerState
  const isDraw = !winner && !activeBoard.includes(null)

  // Bot move logic
  useEffect(() => {
    if (mode === 'bot' && !xIsNext && !winner && !isDraw) {
      setStatus('Bot is thinking...')
      const timer = setTimeout(() => {
        const move = getBestMove([...board])
        const newBoard = [...board]
        newBoard[move] = 'O'
        setBoard(newBoard)
        setXIsNext(true)
      }, 500)
      return () => clearTimeout(timer)
    }
  }, [xIsNext, board, winner, isDraw, mode])

  useEffect(() => {
    if (mode === 'bot') {
      if (winner) setStatus(winner === 'X' ? 'You win! 🎉' : 'Bot wins! 🤖')
      else if (isDraw) setStatus('Draw!')
      else setStatus(xIsNext ? 'Your turn (X)' : 'Bot is thinking...')
    }
  }, [board, xIsNext, winner, isDraw, mode])

  const handleClick = (i) => {
    if (activeBoard[i] || winner) return
    if (!isMyTurn) return

    if (mode === 'bot') {
      const newBoard = [...board]
      newBoard[i] = 'X'
      setBoard(newBoard)
      setXIsNext(false)
    } else {
      onMove(i)
    }
  }

  const reset = () => {
    setBoard(Array(9).fill(null))
    setXIsNext(true)
  }

  return (
    <div className={styles.container}>
      <div className={styles.status}>
        {mode === 'bot' ? status : (
          winner ? `Winner: ${winner}` : isDraw ? 'Draw!' : isMyTurn ? 'Your turn' : 'Waiting for opponent...'
        )}
      </div>
      
      <div className={styles.board}>
        {activeBoard.map((sq, i) => (
          <button 
            key={i} 
            className={`${styles.square} ${sq === 'X' ? styles.x : sq === 'O' ? styles.o : ''}`}
            onClick={() => handleClick(i)}
            disabled={!!sq || !!winner || (mode === 'bot' && !xIsNext)}
          >
            {sq}
          </button>
        ))}
      </div>

      {mode === 'bot' && (winner || isDraw) && (
        <Button size="sm" onClick={reset} style={{ marginTop: 16 }}>Play Again</Button>
      )}
    </div>
  )
}
