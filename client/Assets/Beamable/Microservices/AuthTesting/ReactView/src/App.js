import React from 'react';
import logo from './logo.svg';
import './App.css';

class App extends React.Component {
  constructor(props) {
    super(props);
    this.state = {a: 0, b: 0, sum: 0};
    this.onInputchange = this.onInputchange.bind(this);
    this.add = this.add.bind(this);
  }
  onInputchange(event) {
    this.setState({
      [event.target.name]: event.target.value
    });
  }
  add(){
    this.setState({
      sum: this.state.a + this.state.b
    })
  }
  render(){
    return (
        <div className="App">
          <header className="App-header">
            <img src={logo} className="App-logo" alt="logo" />
            <label>The sum is {this.state.sum}</label>
            <input name="a" value={this.state.a} onChange={this.onInputchange}/>
            <input name="b" value={this.state.b} onChange={this.onInputchange}/>
            <button onClick={this.add}>
              <span> Add It Up </span>
            </button>
          </header>
        </div>
    )
  }
}

//
// function App() {
//   var sum = 0
//   return (

//   );
// }

export default App;
